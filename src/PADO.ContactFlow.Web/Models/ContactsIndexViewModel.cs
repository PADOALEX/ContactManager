namespace PADO.ContactFlow.Web.Models;

public sealed class ContactsIndexViewModel
{
    public ContactQueryParameters Query { get; init; } = new();
    public IReadOnlyList<CsvImportError> ImportErrors { get; init; } = Array.Empty<CsvImportError>();
    public int CsvRowsRead { get; init; }
    public string? ErrorMessage { get; init; }
}
