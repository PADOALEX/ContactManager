using PADO.ContactFlow.Web.Models;

namespace PADO.ContactFlow.Web.Data.Repositories;

public interface IContactRepository
{
    Task<PagedResult<Contact>> GetPagedAsync(ContactQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<int> AddAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<int> AddRangeAsync(IReadOnlyCollection<Contact> contacts, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingPhonesAsync(IReadOnlyCollection<string> phones, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
