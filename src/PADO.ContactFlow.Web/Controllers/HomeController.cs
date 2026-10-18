using System.Diagnostics;
using PADO.ContactFlow.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace PADO.ContactFlow.Web.Controllers;

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
