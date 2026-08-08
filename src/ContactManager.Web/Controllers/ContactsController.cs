using ContactManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Web.Controllers;

public sealed class ContactsController : Controller
{
    public IActionResult Index(
        [FromQuery] ContactQueryParameters query)
    {
        query.Normalize();

        return View(query);
    }
}