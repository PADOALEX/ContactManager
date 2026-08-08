using ContactManager.Web.Data.Repositories;
using ContactManager.Web.Models;
using ContactManager.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Web.Controllers;

public sealed class ContactsController : Controller
{
    private const long MaxCsvFileSize = 5 * 1024 * 1024;

    private readonly ICsvImportService _csvImportService;
    private readonly IContactRepository _contactRepository;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(
        ICsvImportService csvImportService,
        IContactRepository contactRepository,
        ILogger<ContactsController> logger)
    {
        _csvImportService = csvImportService;
        _contactRepository = contactRepository;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(
        [FromQuery] ContactQueryParameters query)
    {
        query.Normalize();

        return View(new ContactsIndexViewModel
        {
            Query = query
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return View("Index", new ContactsIndexViewModel
            {
                ErrorMessage = "Please select a CSV file."
            });
        }

        if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".csv",
                StringComparison.OrdinalIgnoreCase))
        {
            return View("Index", new ContactsIndexViewModel
            {
                ErrorMessage = "Only .csv files are allowed."
            });
        }

        if (file.Length > MaxCsvFileSize)
        {
            return View("Index", new ContactsIndexViewModel
            {
                ErrorMessage =
                    "The CSV file is too large. Maximum size is 5 MB."
            });
        }

        var importResult =
            await _csvImportService.ParseAsync(
                file,
                cancellationToken);

        if (!importResult.IsValid)
        {
            return View("Index", new ContactsIndexViewModel
            {
                ImportErrors = importResult.Errors
            });
        }

        if (importResult.Contacts.Count == 0)
        {
            return View("Index", new ContactsIndexViewModel
            {
                ErrorMessage =
                    "The CSV file does not contain any contacts."
            });
        }

        try
        {
            var insertedCount =
                await _contactRepository.AddRangeAsync(
                    importResult.Contacts);

            TempData["SuccessMessage"] =
                $"Successfully imported {insertedCount} contact(s).";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An error occurred while importing contacts.");

            return View("Index", new ContactsIndexViewModel
            {
                ErrorMessage =
                    "An unexpected error occurred while saving contacts."
            });
        }
    }
}