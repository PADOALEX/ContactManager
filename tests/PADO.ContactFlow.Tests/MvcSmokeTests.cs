using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PADO.ContactFlow.Web.Data.Repositories;
using PADO.ContactFlow.Web.Models;

namespace PADO.ContactFlow.Tests;

public sealed class MvcSmokeTests : IClassFixture<ContactWebFactory>
{
    private readonly ContactWebFactory _factory;
    public MvcSmokeTests(ContactWebFactory factory) => _factory = factory;

    [Fact]
    public async Task GetIndex_RendersSearchAndCreateActions()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("PADO ContactFlow", html);
        Assert.Contains("serverSearch", html);
        Assert.Contains("createContactForm", html);
    }

    [Fact]
    public async Task CreateWithAntiforgeryToken_AcceptsValidContact_RejectsInvalidSalary()
    {
        using var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");
        var token = System.Text.RegularExpressions.Regex.Match(html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token));

        async Task<HttpResponseMessage> SendAsync(decimal salary)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/Contacts/Create");
            request.Headers.Add("RequestVerificationToken", token);
            request.Content = System.Net.Http.Json.JsonContent.Create(new
            {
                name = "Alice", dateOfBirth = "2000-01-01", married = false,
                phone = "+441234", salary
            });
            return await client.SendAsync(request);
        }

        using var valid = await SendAsync(100.50m);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        using var invalid = await SendAsync(-1m);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task UpdateAndDelete_WithAntiforgeryToken_AcceptValidRequests()
    {
        using var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");
        var token = System.Text.RegularExpressions.Regex.Match(html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);

        using var update = new HttpRequestMessage(HttpMethod.Put, "/Contacts/Update");
        update.Headers.Add("RequestVerificationToken", token);
        update.Content = System.Net.Http.Json.JsonContent.Create(new
        {
            id = 42, name = "Alice", dateOfBirth = "2000-01-01", married = false,
            phone = "+441234", salary = "1234567890123456.78"
        });
        using var updated = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        using var invalidUpdate = new HttpRequestMessage(HttpMethod.Put, "/Contacts/Update");
        invalidUpdate.Headers.Add("RequestVerificationToken", token);
        invalidUpdate.Content = System.Net.Http.Json.JsonContent.Create(new
        {
            id = 42, name = "Alice", dateOfBirth = "2000-01-01", married = false,
            phone = "+441234", salary = "-1.234"
        });
        using var rejected = await client.SendAsync(invalidUpdate);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        using var delete = new HttpRequestMessage(HttpMethod.Delete, "/Contacts/Delete");
        delete.Headers.Add("RequestVerificationToken", token);
        delete.Content = System.Net.Http.Json.JsonContent.Create(new { id = 42 });
        using var deleted = await client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
    }

    [Fact]
    public async Task UnsafeMethodsWithoutAntiforgeryToken_AreRejected()
    {
        using var client = _factory.CreateClient();
        using var update = new HttpRequestMessage(HttpMethod.Put, "/Contacts/Update");
        update.Content = System.Net.Http.Json.JsonContent.Create(new { id = 42 });
        using var updateResponse = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        using var delete = new HttpRequestMessage(HttpMethod.Delete, "/Contacts/Delete");
        delete.Content = System.Net.Http.Json.JsonContent.Create(new { id = 42 });
        using var deleteResponse = await client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateWithoutAntiforgeryToken_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/Contacts/Create",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class ContactWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../../src/PADO.ContactFlow.Web")));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IContactRepository>();
            services.AddSingleton<IContactRepository>(new EmptyRepository());
        });
    }

    private sealed class EmptyRepository : IContactRepository
    {
        public Task<PagedResult<Contact>> GetPagedAsync(ContactQueryParameters p, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<Contact> { Items = [], Page = 1, PageSize = 10, TotalCount = 0 });
        public Task<int> AddAsync(Contact c, CancellationToken ct = default) => Task.FromResult(1);
        public Task<int> AddRangeAsync(IReadOnlyCollection<Contact> c, CancellationToken ct = default) => Task.FromResult(c.Count);
        public Task<IReadOnlySet<string>> GetExistingPhonesAsync(IReadOnlyCollection<string> p, CancellationToken ct = default)
            => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
        public Task<bool> UpdateAsync(Contact c, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
    }
}
