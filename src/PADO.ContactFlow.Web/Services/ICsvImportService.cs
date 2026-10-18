using PADO.ContactFlow.Web.Models;

namespace PADO.ContactFlow.Web.Services;

public interface ICsvImportService
{
    Task<CsvImportResult> ParseAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);
}