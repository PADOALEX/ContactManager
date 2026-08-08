using ContactManager.Web.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Web.ViewComponents;

public sealed class ContactListViewComponent : ViewComponent
{
    private readonly IContactRepository _contactRepository;

    public ContactListViewComponent(
        IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var contacts = await _contactRepository.GetAllAsync();

        return View(contacts);
    }
}