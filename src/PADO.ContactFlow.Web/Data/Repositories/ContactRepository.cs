using Dapper;
using PADO.ContactFlow.Web.Models;

namespace PADO.ContactFlow.Web.Data.Repositories;

public sealed class ContactRepository(ISqlConnectionFactory connectionFactory) : IContactRepository
{
    private const string InsertSql = """
        INSERT INTO dbo.Contacts ([Name], DateOfBirth, Married, Phone, Salary)
        OUTPUT INSERTED.Id
        VALUES (@Name, @DateOfBirth, @Married, @Phone, @Salary);
        """;

    public async Task<PagedResult<Contact>> GetPagedAsync(ContactQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        parameters.Normalize();
        var sortColumn = SortColumnSql(parameters.SortBy);
        var direction = parameters.SortDirection == SortDirection.Asc ? "ASC" : "DESC";
        var secondary = parameters.SortBy == ContactSortColumn.Id ? "" : ", Id DESC";
        var column = FilterColumnSql(parameters.FilterBy);
        var where = parameters.Search is null ? "" : column is null
            ? "WHERE (" + string.Join(" OR ", AllSearchColumns.Select(c => $"{c} LIKE @Pattern ESCAPE '\\'")) + ")"
            : $"WHERE {column} LIKE @Pattern ESCAPE '\\'";

        var args = new DynamicParameters();
        args.Add("Pattern", parameters.Search is null ? null : $"%{EscapeLike(parameters.Search)}%");
        var countSql = $"SELECT COUNT(*) FROM dbo.Contacts {where};";

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, args, cancellationToken: cancellationToken));
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)parameters.PageSize));
        var page = Math.Clamp(parameters.Page, 1, totalPages);
        args.Add("Offset", (page - 1) * parameters.PageSize);
        args.Add("PageSize", parameters.PageSize);

        var sql = $"""
            SELECT Id, [Name], DateOfBirth, Married, Phone, Salary
            FROM dbo.Contacts {where}
            ORDER BY {sortColumn} {direction}{secondary}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
        var contacts = await connection.QueryAsync<Contact>(
            new CommandDefinition(sql, args, cancellationToken: cancellationToken));

        return new PagedResult<Contact>
        {
            Items = contacts.AsList(), Page = page,
            PageSize = parameters.PageSize, TotalCount = totalCount
        };
    }

    public async Task<int> AddAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(InsertSql, contact, cancellationToken: cancellationToken));
    }

    public async Task<int> AddRangeAsync(IReadOnlyCollection<Contact> contacts, CancellationToken cancellationToken = default)
    {
        if (contacts.Count == 0) return 0;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var count = 0;
            foreach (var contact in contacts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    InsertSql, contact, transaction, cancellationToken: cancellationToken));
                count++;
            }
            await transaction.CommitAsync(cancellationToken);
            return count;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlySet<string>> GetExistingPhonesAsync(
        IReadOnlyCollection<string> phones, CancellationToken cancellationToken = default)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (phones.Count == 0) return found;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        foreach (var batch in phones.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(500))
        {
            var matches = await connection.QueryAsync<string>(new CommandDefinition(
                "SELECT Phone FROM dbo.Contacts WHERE Phone IN @Phones;",
                new { Phones = batch }, cancellationToken: cancellationToken));
            foreach (var match in matches) found.Add(match.Trim());
        }
        return found;
    }

    public async Task<bool> UpdateAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Contacts
            SET [Name]=@Name, DateOfBirth=@DateOfBirth, Married=@Married,
                Phone=@Phone, Salary=@Salary
            WHERE Id=@Id;
            """;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, contact, cancellationToken: cancellationToken));
        return rows == 1;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM dbo.Contacts WHERE Id=@Id;", new { Id=id }, cancellationToken: cancellationToken));
        return rows == 1;
    }

    private static readonly string[] AllSearchColumns =
    [
        "[Name]", "CONVERT(NVARCHAR(10), DateOfBirth, 23)",
        "CASE WHEN Married = 1 THEN N'Yes' ELSE N'No' END",
        "Phone", "CONVERT(NVARCHAR(40), Salary)"
    ];

    private static string? FilterColumnSql(ContactFilterColumn filter) => filter switch
    {
        ContactFilterColumn.Name => "[Name]",
        ContactFilterColumn.DateOfBirth => "CONVERT(NVARCHAR(10), DateOfBirth, 23)",
        ContactFilterColumn.Married => "CASE WHEN Married = 1 THEN N'Yes' ELSE N'No' END",
        ContactFilterColumn.Phone => "Phone",
        ContactFilterColumn.Salary => "CONVERT(NVARCHAR(40), Salary)",
        _ => null
    };

    private static string SortColumnSql(ContactSortColumn sort) => sort switch
    {
        ContactSortColumn.Name => "[Name]",
        ContactSortColumn.DateOfBirth => "DateOfBirth",
        ContactSortColumn.Married => "Married",
        ContactSortColumn.Phone => "Phone",
        ContactSortColumn.Salary => "Salary",
        _ => "Id"
    };

    // Escape SQL Server LIKE wildcards (including an existing backslash) before parameter binding.
    internal static string EscapeLike(string text) => text
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);
}
