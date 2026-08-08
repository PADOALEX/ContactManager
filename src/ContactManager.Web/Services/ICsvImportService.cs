using ContactManager.Web.Models;

namespace ContactManager.Web.Services;

public interface ICsvImportService
{
    Task<CsvImportResult> ParseAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);
}