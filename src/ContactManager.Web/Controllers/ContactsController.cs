using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Web.Controllers;

public sealed class ContactsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}