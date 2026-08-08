namespace ContactManager.Web.Models;

public sealed class CsvImportResult
{
    public List<Contact> Contacts { get; } = [];

    public List<CsvImportError> Errors { get; } = [];

    public bool IsValid => Errors.Count == 0;
}