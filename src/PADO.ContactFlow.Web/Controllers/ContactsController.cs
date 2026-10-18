using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PADO.ContactFlow.Web.Data.Repositories;
using PADO.ContactFlow.Web.Models;
using PADO.ContactFlow.Web.Services;

namespace PADO.ContactFlow.Web.Controllers;

public sealed class ContactsController(
    ICsvImportService csvImportService,
    IContactRepository contactRepository,
    ILogger<ContactsController> logger) : Controller
{
    private const long MaxCsvFileSize = 5 * 1024 * 1024;

    [HttpGet]
    public IActionResult Index([FromQuery] ContactQueryParameters query)
    {
        query.Normalize();
        return View(new ContactsIndexViewModel { Query = query });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return ImportError("Select a non-empty CSV file.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            return ImportError("Only .csv files are accepted.");
        if (file.Length > MaxCsvFileSize)
            return ImportError("CSV exceeds the 5 MB limit.");

        CsvImportResult parsed;
        try
        {
            parsed = await csvImportService.ParseAsync(file, cancellationToken);
            if (!parsed.IsValid)
                return View("Index", new ContactsIndexViewModel
                {
                    ImportErrors = parsed.Errors, CsvRowsRead = parsed.DataRowsRead
                });
            if (parsed.Contacts.Count == 0)
                return ImportError("CSV has no data rows.");

            // Friendly preflight errors; the unique database index protects against concurrent imports.
            var existing = await contactRepository.GetExistingPhonesAsync(
                parsed.Contacts.Select(c => c.Phone).ToArray(), cancellationToken);
            foreach (var row in parsed.Rows.Where(row => existing.Contains(row.Contact.Phone)))
                parsed.Errors.Add(new CsvImportError
                {
                    RowNumber = row.LineNumber,
                    Message = $"Phone '{row.Contact.Phone}' already exists in the database."
                });
            if (!parsed.IsValid)
                return View("Index", new ContactsIndexViewModel
                {
                    ImportErrors = parsed.Errors, CsvRowsRead = parsed.DataRowsRead
                });

            var inserted = await contactRepository.AddRangeAsync(parsed.Contacts, cancellationToken);
            logger.LogInformation("CSV import completed: {ImportedCount} records.", inserted);
            TempData["SuccessMessage"] = $"Imported {inserted} of {parsed.DataRowsRead} records. No rows were skipped (atomic import).";
            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (SqlException ex) when (IsDuplicate(ex))
        {
            logger.LogWarning("CSV import rejected by unique phone constraint.");
            return ImportError("Import was rolled back: a phone already exists. Refresh the data and retry.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CSV import failed.");
            return ImportError("Import failed; no contacts were saved. Check server logs for details.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] ContactCreateRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(new { message = "Invalid request." });
        if (!ModelState.IsValid) return ValidationError();
        try
        {
            var id = await contactRepository.AddAsync(request.ToContact(), cancellationToken);
            return Ok(new { id, message = "Contact created successfully." });
        }
        catch (SqlException ex) when (IsDuplicate(ex))
        {
            return Conflict(new { message = "A contact with this phone already exists." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create contact.");
            return StatusCode(500, new { message = "Unable to create contact." });
        }
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] ContactUpdateRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(new { message = "Invalid request." });
        if (!ModelState.IsValid) return ValidationError();
        try
        {
            var updated = await contactRepository.UpdateAsync(request.ToContact(request.Id), cancellationToken);
            return updated ? Ok(new { message = "Contact updated successfully." })
                : NotFound(new { message = "Contact not found." });
        }
        catch (SqlException ex) when (IsDuplicate(ex))
        {
            return Conflict(new { message = "A contact with this phone already exists." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update contact {ContactId}.", request.Id);
            return StatusCode(500, new { message = "Unable to update contact." });
        }
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] DeleteContactRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest(new { message = "Invalid request." });
        if (!ModelState.IsValid) return ValidationError();
        try
        {
            var deleted = await contactRepository.DeleteAsync(request.Id, cancellationToken);
            return deleted ? Ok(new { message = "Contact deleted successfully." })
                : NotFound(new { message = "Contact not found." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete contact {ContactId}.", request.Id);
            return StatusCode(500, new { message = "Unable to delete contact." });
        }
    }

    private IActionResult ImportError(string message) => View("Index", new ContactsIndexViewModel { ErrorMessage = message });

    private IActionResult ValidationError() => BadRequest(new
    {
        message = "Validation failed.",
        errors = ModelState.Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(err =>
                string.IsNullOrWhiteSpace(err.ErrorMessage) ? "Invalid value." : err.ErrorMessage).ToArray())
    });

    private static bool IsDuplicate(SqlException ex) => ex.Number is 2601 or 2627;
}
