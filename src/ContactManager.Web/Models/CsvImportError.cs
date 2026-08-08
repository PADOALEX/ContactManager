namespace ContactManager.Web.Models;

public sealed class CsvImportError
{
    public int RowNumber { get; init; }

    public required string Message { get; init; }
}