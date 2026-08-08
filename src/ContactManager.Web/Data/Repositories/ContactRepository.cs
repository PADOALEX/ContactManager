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

    public async Task<IReadOnlyList<Contact>> GetAllAsync()
    {
        const string sql = """
            SELECT
                Id,
                [Name],
                DateOfBirth,
                Married,
                Phone,
                Salary
            FROM dbo.Contacts
            ORDER BY Id DESC;
            """;

        await using var connection = _connectionFactory.CreateConnection();

        await connection.OpenAsync();

        var contacts = await connection.QueryAsync<Contact>(sql);

        return contacts.AsList();
    }
}