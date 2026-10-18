using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using PADO.ContactFlow.Web.Controllers;
using PADO.ContactFlow.Web.Data.Repositories;
using PADO.ContactFlow.Web.Models;
using PADO.ContactFlow.Web.Services;

namespace PADO.ContactFlow.Tests;

public sealed class CsvControllerTests
{
    private const string Csv = "Name,Date of birth,Married,Phone,Salary\nAlice,2000-01-01,no,+123,100.00\n";

    [Fact]
    public async Task DuplicatePhoneInDatabase_PreventsAnyInsert()
    {
        var repo = new FakeRepository { Existing = new HashSet<string> { "+123" } };
        var controller = Controller(repo);
        var result = await controller.Import(File(Csv), CancellationToken.None);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ContactsIndexViewModel>(view.Model);
        Assert.Contains(model.ImportErrors, e => e.RowNumber == 2 && e.Message.Contains("already exists"));
        Assert.Equal(0, repo.AddRangeCalls);
    }

    [Fact]
    public async Task InvalidCsv_PreventsDatabaseWrite()
    {
        var repo = new FakeRepository();
        var result = await Controller(repo).Import(File(Csv.Replace("100.00", "-3.00")), CancellationToken.None);
        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, repo.AddRangeCalls);
    }

    [Fact]
    public async Task ValidCsv_InsertsOnceAndRedirects()
    {
        var repo = new FakeRepository();
        var controller = Controller(repo);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempDataProvider());
        var result = await controller.Import(File(Csv), CancellationToken.None);
        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, repo.AddRangeCalls);
        Assert.Contains("Imported 1 of 1", controller.TempData["SuccessMessage"]?.ToString());
    }

    private static ContactsController Controller(FakeRepository repo) =>
        new(new CsvImportService(), repo, NullLogger<ContactsController>.Instance);

    private static FormFile File(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "sample.csv");
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class FakeRepository : IContactRepository
    {
        public IReadOnlySet<string> Existing { get; set; } = new HashSet<string>();
        public int AddRangeCalls { get; private set; }
        public Task<PagedResult<Contact>> GetPagedAsync(ContactQueryParameters q, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<Contact> { Items = [], Page = 1, PageSize = 10, TotalCount = 0 });
        public Task<int> AddAsync(Contact contact, CancellationToken ct = default) => Task.FromResult(1);
        public Task<int> AddRangeAsync(IReadOnlyCollection<Contact> contacts, CancellationToken ct = default)
        {
            AddRangeCalls++;
            return Task.FromResult(contacts.Count);
        }
        public Task<IReadOnlySet<string>> GetExistingPhonesAsync(IReadOnlyCollection<string> phones, CancellationToken ct = default)
            => Task.FromResult(Existing);
        public Task<bool> UpdateAsync(Contact contact, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
    }
}
