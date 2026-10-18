using PADO.ContactFlow.Web.Data.Repositories;
using PADO.ContactFlow.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace PADO.ContactFlow.Web.ViewComponents;

public sealed class ContactListViewComponent : ViewComponent
{
    private readonly IContactRepository _contactRepository;

    public ContactListViewComponent(
        IContactRepository contactRepository)
    {
        _contactRepository = contactRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        ContactQueryParameters query)
    {
        var result =
            await _contactRepository.GetPagedAsync(query, HttpContext.RequestAborted);

        var viewModel = new ContactListViewModel
        {
            Result = result,
            Query = query
        };

        return View(viewModel);
    }
}