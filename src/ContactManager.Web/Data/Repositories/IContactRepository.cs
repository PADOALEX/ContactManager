using ContactManager.Web.Models;

namespace ContactManager.Web.Data.Repositories;

public interface IContactRepository
{
    Task<IReadOnlyList<Contact>> GetAllAsync();
}