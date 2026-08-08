using ContactManager.Web.Data.Repositories;
using ContactManager.Web.Models;
using ContactManager.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Web.Controllers;

public sealed class ContactsController : Controller
{
    private const long MaxCsvFileSize = 5 * 1024 * 1024;
    private const decimal MaxSalary = 9999999999999999.99m;

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

        CsvImportResult importResult;

        try
        {
            importResult = await _csvImportService.ParseAsync(
                file,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An error occurred while reading CSV file {FileName}.",
                file.FileName);

            return View("Index", new ContactsIndexViewModel
            {
                ErrorMessage =
                    "The CSV file could not be read. Check its format and try again."
            });
        }

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
                "An error occurred while saving imported contacts.");

            return View("Index", new ContactsIndexViewModel
            {
                ErrorMessage =
                    "An unexpected error occurred while saving contacts."
            });
        }
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        [FromBody] ContactUpdateRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new
            {
                message = "Invalid request."
            });
        }

        if (request.DateOfBirth.HasValue &&
            request.DateOfBirth.Value.Date > DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(request.DateOfBirth),
                "Date of birth cannot be in the future.");
        }

        if (request.Salary.HasValue)
        {
            if (request.Salary.Value < 0)
            {
                ModelState.AddModelError(
                    nameof(request.Salary),
                    "Salary cannot be negative.");
            }
            else if (request.Salary.Value > MaxSalary)
            {
                ModelState.AddModelError(
                    nameof(request.Salary),
                    "Salary exceeds the supported database range.");
            }
            else if (decimal.Round(request.Salary.Value, 2)
                != request.Salary.Value)
            {
                ModelState.AddModelError(
                    nameof(request.Salary),
                    "Salary can contain at most 2 decimal places.");
            }
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                message = "Validation failed.",
                errors = ModelState
                    .Where(entry =>
                        entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors
                            .Select(error =>
                                string.IsNullOrWhiteSpace(
                                    error.ErrorMessage)
                                    ? "Invalid value."
                                    : error.ErrorMessage)
                            .ToArray())
            });
        }

        var contact = new Contact
        {
            Id = request.Id,
            Name = request.Name.Trim(),
            DateOfBirth = request.DateOfBirth!.Value.Date,
            Married = request.Married,
            Phone = request.Phone.Trim(),
            Salary = request.Salary!.Value
        };

        try
        {
            var updated =
                await _contactRepository.UpdateAsync(contact);

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Contact was not found."
                });
            }

            return Ok(new
            {
                message = "Contact updated successfully."
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to update contact {ContactId}.",
                request.Id);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "An unexpected error occurred while updating the contact."
                });
        }
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        [FromBody] DeleteContactRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new
            {
                message = "Invalid request."
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                message = "Invalid contact id."
            });
        }

        try
        {
            var deleted =
                await _contactRepository.DeleteAsync(request.Id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Contact was not found."
                });
            }

            return Ok(new
            {
                message = "Contact deleted successfully."
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to delete contact {ContactId}.",
                request.Id);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "An unexpected error occurred while deleting the contact."
                });
        }
    }
}
