using ContactManager.Web.Models;
using Dapper;

namespace ContactManager.Web.Data.Repositories;

public sealed class ContactRepository : IContactRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ContactRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<Contact>> GetPagedAsync(
        ContactQueryParameters parameters)
    {
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var sortColumn = GetSortColumn(parameters.SortBy);

        var sortDirection =
            parameters.SortDirection == SortDirection.Asc
                ? "ASC"
                : "DESC";

        const string countSql = """
            SELECT COUNT(*)
            FROM dbo.Contacts;
            """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        var totalCount =
            await connection.ExecuteScalarAsync<int>(countSql);

        var totalPages =
            totalCount == 0
                ? 1
                : (int)Math.Ceiling(totalCount / (double)pageSize);

        var page = Math.Clamp(
            parameters.Page,
            1,
            totalPages);

        var offset = (page - 1) * pageSize;

        var secondarySort =
            parameters.SortBy == ContactSortColumn.Id
                ? string.Empty
                : ", Id DESC";

        var sql = $"""
            SELECT
                Id,
                [Name],
                DateOfBirth,
                Married,
                Phone,
                Salary
            FROM dbo.Contacts
            ORDER BY {sortColumn} {sortDirection}{secondarySort}
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        var contacts =
            await connection.QueryAsync<Contact>(
                sql,
                new
                {
                    Offset = offset,
                    PageSize = pageSize
                });

        return new PagedResult<Contact>
        {
            Items = contacts.AsList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<int> AddRangeAsync(
        IReadOnlyCollection<Contact> contacts)
    {
        if (contacts.Count == 0)
        {
            return 0;
        }

        const string sql = """
            INSERT INTO dbo.Contacts
            (
                [Name],
                DateOfBirth,
                Married,
                Phone,
                Salary
            )
            VALUES
            (
                @Name,
                @DateOfBirth,
                @Married,
                @Phone,
                @Salary
            );
            """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            var insertedCount =
                await connection.ExecuteAsync(
                    sql,
                    contacts,
                    transaction);

            await transaction.CommitAsync();

            return insertedCount;
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }

    public async Task<bool> UpdateAsync(Contact contact)
    {
        const string sql = """
            UPDATE dbo.Contacts
            SET
                [Name] = @Name,
                DateOfBirth = @DateOfBirth,
                Married = @Married,
                Phone = @Phone,
                Salary = @Salary
            WHERE Id = @Id;
            """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        var affectedRows =
            await connection.ExecuteAsync(
                sql,
                contact);

        return affectedRows == 1;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = """
            DELETE FROM dbo.Contacts
            WHERE Id = @Id;
            """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        var affectedRows =
            await connection.ExecuteAsync(
                sql,
                new { Id = id });

        return affectedRows == 1;
    }

    private static string GetSortColumn(
        ContactSortColumn sortBy)
    {
        return sortBy switch
        {
            ContactSortColumn.Name => "[Name]",
            ContactSortColumn.DateOfBirth => "DateOfBirth",
            ContactSortColumn.Married => "Married",
            ContactSortColumn.Phone => "Phone",
            ContactSortColumn.Salary => "Salary",
            _ => "Id"
        };
    }
}