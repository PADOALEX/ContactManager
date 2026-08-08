using System.Diagnostics;
using ContactManager.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Web.Controllers;

public sealed class HomeController : Controller
{
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id
                ?? HttpContext.TraceIdentifier
        });
    }
}
