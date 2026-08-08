namespace ContactManager.Web.Models;

public sealed class ContactsIndexViewModel
{
    public ContactQueryParameters Query { get; init; } = new();

    public IReadOnlyList<CsvImportError> ImportErrors { get; init; }
        = Array.Empty<CsvImportError>();

    public string? ErrorMessage { get; init; }
}