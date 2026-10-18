using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PADO.ContactFlow.Web.Models;

namespace PADO.ContactFlow.Web.Services;

public sealed class CsvImportService : ICsvImportService
{
    public const int MaxRows = 10_000;
    private static readonly string[] Headers = ["Name", "Date of birth", "Married", "Phone", "Salary"];
    private static readonly string[] Dates = ["yyyy-MM-dd", "dd.MM.yyyy"];

    public async Task<CsvImportResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var result = new CsvImportResult();
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true),
            detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: false);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            HasHeaderRecord = true,
            DetectColumnCountChanges = false,
            BadDataFound = args => throw new FormatException($"Malformed CSV near line {args.Context.Parser?.Row ?? 1}.")
        };
        using var csv = new CsvReader(reader, config);

        try
        {
            if (!await csv.ReadAsync())
            {
                Error(result, 1, "CSV file is empty.");
                return result;
            }

            csv.ReadHeader();
            var headerRecord = csv.HeaderRecord ?? [];
            var normalized = headerRecord.Select(h => h.Trim().TrimStart('\uFEFF')).ToArray();
            if (normalized.Length != Headers.Length ||
                normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length ||
                Headers.Any(h => !normalized.Contains(h, StringComparer.OrdinalIgnoreCase)))
            {
                Error(result, 1, "Headers must be exactly: Name, Date of birth, Married, Phone, Salary (any order, case-insensitive; no duplicates or extra columns).");
                return result;
            }

            var indexes = Headers.Select(h => Array.FindIndex(normalized,
                column => string.Equals(column, h, StringComparison.OrdinalIgnoreCase))).ToArray();

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.DataRowsRead++;
                var line = csv.Context.Parser?.Row ?? (result.DataRowsRead + 1);
                if (result.DataRowsRead > MaxRows)
                {
                    Error(result, line, $"CSV exceeds the limit of {MaxRows} data rows.");
                    break;
                }
                var record = csv.Context.Parser?.Record ?? [];
                if (record.Length != headerRecord.Length)
                {
                    Error(result, line, $"Expected {headerRecord.Length} fields, found {record.Length}.");
                    continue;
                }

                var name = record[indexes[0]].Trim();
                var dateText = record[indexes[1]].Trim();
                var marriedText = record[indexes[2]].Trim();
                var phone = record[indexes[3]].Trim();
                var salaryText = record[indexes[4]].Trim();
                var valid = true;

                if (name.Length == 0 || name.Length > ContactRules.NameMaxLength)
                {
                    Error(result, line, "Name is required and must be at most 200 characters."); valid = false;
                }
                if (!DateTime.TryParseExact(dateText, Dates, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                {
                    Error(result, line, "Date of birth must use yyyy-MM-dd or dd.MM.yyyy."); valid = false;
                }
                else if (dob.Date > DateTime.Today)
                {
                    Error(result, line, "Date of birth cannot be in the future."); valid = false;
                }
                if (!TryBoolean(marriedText, out var married))
                {
                    Error(result, line, "Married must be true/false, yes/no, or 1/0."); valid = false;
                }
                if (phone.Length == 0 || phone.Length > ContactRules.PhoneMaxLength)
                {
                    Error(result, line, "Phone is required and must be at most 50 characters."); valid = false;
                }
                var salaryCulture = salaryText.Contains(',') && !salaryText.Contains('.')
                    ? CultureInfo.GetCultureInfo("uk-UA") : CultureInfo.InvariantCulture;
                if (!decimal.TryParse(salaryText, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    salaryCulture, out var salary))
                {
                    Error(result, line, "Salary must be a valid decimal (use a dot or a quoted comma separator)."); valid = false;
                }
                else if (salary < 0 || salary > ContactRules.MaxSalary || decimal.Round(salary, 2) != salary)
                {
                    Error(result, line, "Salary must be non-negative, fit DECIMAL(18,2), and have at most 2 decimal places."); valid = false;
                }

                if (phone.Length > 0 && phone.Length <= ContactRules.PhoneMaxLength)
                {
                    if (seen.TryGetValue(phone, out var firstLine))
                    {
                        Error(result, line, $"Duplicate phone '{phone}' (first found on line {firstLine})."); valid = false;
                    }
                    else seen.Add(phone, line);
                }

                if (!valid) continue;
                var contact = new Contact
                {
                    Name = name, DateOfBirth = dob.Date, Married = married,
                    Phone = phone, Salary = salary
                };
                result.Contacts.Add(contact);
                result.Rows.Add(new CsvImportRow(line, contact));
            }
        }
        catch (DecoderFallbackException)
        {
            Error(result, csv.Context.Parser?.Row ?? 1, "File is not valid UTF-8 (UTF-8 BOM is supported).");
        }
        catch (CsvHelperException)
        {
            Error(result, csv.Context.Parser?.Row ?? 1, "Malformed CSV: check field counts and quoting.");
        }
        catch (FormatException ex)
        {
            Error(result, csv.Context.Parser?.Row ?? 1, ex.Message);
        }
        return result;
    }

    private static bool TryBoolean(string text, out bool value)
    {
        switch (text.ToLowerInvariant())
        {
            case "true": case "yes": case "1": value = true; return true;
            case "false": case "no": case "0": value = false; return true;
            default: value = false; return false;
        }
    }

    private static void Error(CsvImportResult result, int line, string message) =>
        result.Errors.Add(new CsvImportError { RowNumber = line, Message = message });
}
