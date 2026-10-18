namespace PADO.ContactFlow.Web.Models;

public sealed class CsvImportResult
{
    public List<Contact> Contacts { get; } = [];
    public List<CsvImportError> Errors { get; } = [];
    public List<CsvImportRow> Rows { get; } = [];
    public int DataRowsRead { get; set; }
    public bool IsValid => Errors.Count == 0;
}

public sealed record CsvImportRow(int LineNumber, Contact Contact);
