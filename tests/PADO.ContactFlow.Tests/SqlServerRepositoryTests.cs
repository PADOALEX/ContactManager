using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PADO.ContactFlow.Web.Data;
using PADO.ContactFlow.Web.Data.Repositories;
using PADO.ContactFlow.Web.Models;

namespace PADO.ContactFlow.Tests;

// An isolated, randomly named database is created and dropped by every SQL test.
// Set CONTACTFLOW_TEST_CONNECTION to an administrative connection to SQL Server/master.
public sealed class SqlServerRepositoryTests
{
    [Fact, Trait("Category", "SqlServer")]
    public async Task CrudSearchAndRollback_AreCorrectAgainstRealSqlServer()
    {
        var connection = Environment.GetEnvironmentVariable("CONTACTFLOW_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set CONTACTFLOW_TEST_CONNECTION to a connection string with CREATE DATABASE permission.");
        var dbName = "PadoContactFlowTests_" + Guid.NewGuid().ToString("N");
        var masterBuilder = new SqlConnectionStringBuilder(connection) { InitialCatalog = "master" };
        var testBuilder = new SqlConnectionStringBuilder(connection) { InitialCatalog = dbName };
        await using var master = new SqlConnection(masterBuilder.ConnectionString);
        await master.OpenAsync();
        try
        {
            await using (var cmd = new SqlCommand($"CREATE DATABASE [{dbName}]", master)) await cmd.ExecuteNonQueryAsync();
            await using (var db = new SqlConnection(testBuilder.ConnectionString))
            {
                await db.OpenAsync();
                foreach (var name in new[] { "002_CreateContactsTable.sql", "003_UniquePhone.sql" })
                {
                    var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                        "../../../../../Database", name));
                    var sql = await File.ReadAllTextAsync(path);
                    sql = sql.Replace("USE [ContactManagerDb];", "", StringComparison.OrdinalIgnoreCase);
                    foreach (var batch in System.Text.RegularExpressions.Regex.Split(sql, @"(?im)^\s*GO\s*$"))
                    {
                        if (string.IsNullOrWhiteSpace(batch)) continue;
                        await using var command = new SqlCommand(batch, db);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = testBuilder.ConnectionString
            }).Build();
            var repo = new ContactRepository(new SqlConnectionFactory(config));
            var alice = NewContact("Alice", "PH001");
            var id = await repo.AddAsync(alice);
            Assert.True(id > 0);
            var page = await repo.GetPagedAsync(new ContactQueryParameters { Search = "Alice" });
            Assert.Single(page.Items);
            Assert.Equal(1, page.TotalCount);
            alice.Id = id; alice.Name = "Updated";
            Assert.True(await repo.UpdateAsync(alice));
            Assert.False(await repo.UpdateAsync(new Contact { Id = -123, Name = "Missing", Phone = "PHX", DateOfBirth = new DateTime(2000, 1, 1), Salary = 1m }));
            Assert.Contains("PH001", await repo.GetExistingPhonesAsync(["PH001", "NONE"]));
            var percentId = await repo.AddAsync(NewContact("100% Real", "PH003"));
            var literalPercent = await repo.GetPagedAsync(new ContactQueryParameters
            {
                Search = "%", FilterBy = ContactFilterColumn.Name
            });
            Assert.Single(literalPercent.Items);
            Assert.Equal(percentId, literalPercent.Items[0].Id);
            var injectionAttempt = await repo.GetPagedAsync(new ContactQueryParameters
            {
                Search = "' OR 1=1 --", FilterBy = ContactFilterColumn.Name
            });
            Assert.Equal(0, injectionAttempt.TotalCount);

            // DECIMAL(18,2) must survive the SQL round trip without floating-point conversion.
            var preciseContact = NewContact("Precise Salary", "PH004");
            preciseContact.Salary = 1234567890123456.78m;
            var preciseId = await repo.AddAsync(preciseContact);
            var precisePage = await repo.GetPagedAsync(new ContactQueryParameters
            {
                Search = "PH004", FilterBy = ContactFilterColumn.Phone
            });
            Assert.Equal(1234567890123456.78m, Assert.Single(precisePage.Items).Salary);
            Assert.True(await repo.DeleteAsync(preciseId));

            // First insertion must be rolled back after a unique-phone violation on the second.
            await Assert.ThrowsAsync<SqlException>(() => repo.AddRangeAsync([
                NewContact("Bob", "PH002"), NewContact("Collision", "PH001")
            ]));
            Assert.Equal(2, (await repo.GetPagedAsync(new ContactQueryParameters())).TotalCount);
            Assert.True(await repo.DeleteAsync(percentId));
            Assert.True(await repo.DeleteAsync(id));
            Assert.False(await repo.DeleteAsync(id));
        }
        finally
        {
            await using var drop = new SqlCommand($"""
                IF DB_ID(N'{dbName}') IS NOT NULL BEGIN
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{dbName}];
                END
                """, master);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static Contact NewContact(string name, string phone) => new()
    {
        Name = name, Phone = phone, DateOfBirth = new DateTime(2000, 1, 1), Married = false, Salary = 10m
    };
}
