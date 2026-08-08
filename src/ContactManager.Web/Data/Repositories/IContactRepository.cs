using ContactManager.Web.Models;

namespace ContactManager.Web.Data.Repositories;

public interface IContactRepository
{
    Task<PagedResult<Contact>> GetPagedAsync(
        ContactQueryParameters parameters);

    Task<int> AddRangeAsync(
        IReadOnlyCollection<Contact> contacts);
}