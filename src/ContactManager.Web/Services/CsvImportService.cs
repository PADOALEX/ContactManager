using System.Globalization;
using ContactManager.Web.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace ContactManager.Web.Services;

public sealed class CsvImportService : ICsvImportService
{
    private const decimal MaxSalary = 9999999999999999.99m;

    private static readonly string[] RequiredHeaders =
    [
        "Name",
        "Date of birth",
        "Married",
        "Phone",
        "Salary"
    ];

    private static readonly string[] SupportedDateFormats =
    [
        "yyyy-MM-dd",
        "dd.MM.yyyy"
    ];

    public async Task<CsvImportResult> ParseAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var result = new CsvImportResult();

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        var configuration =
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true
            };

        using var csv = new CsvReader(reader, configuration);

        try
        {
            if (!await csv.ReadAsync())
            {
                result.Errors.Add(new CsvImportError
                {
                    RowNumber = 1,
                    Message = "CSV file is empty."
                });

                return result;
            }

            csv.ReadHeader();

            var headers = csv.HeaderRecord ?? [];

            if (headers.Length !=
                headers.Distinct(StringComparer.Ordinal).Count())
            {
                result.Errors.Add(new CsvImportError
                {
                    RowNumber = 1,
                    Message = "CSV contains duplicate column names."
                });
            }

            foreach (var requiredHeader in RequiredHeaders)
            {
                if (!headers.Contains(
                        requiredHeader,
                        StringComparer.Ordinal))
                {
                    result.Errors.Add(new CsvImportError
                    {
                        RowNumber = 1,
                        Message =
                            $"Required column '{requiredHeader}' is missing."
                    });
                }
            }

            if (!result.IsValid)
            {
                return result;
            }

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowNumber =
                    csv.Context.Parser?.Row ?? 1;

                var name =
                    csv.GetField("Name")?.Trim();

                var dateOfBirthRaw =
                    csv.GetField("Date of birth")?.Trim();

                var marriedRaw =
                    csv.GetField("Married")?.Trim();

                var phone =
                    csv.GetField("Phone")?.Trim();

                var salaryRaw =
                    csv.GetField("Salary")?.Trim();

                var rowHasErrors = false;

                if (string.IsNullOrWhiteSpace(name))
                {
                    AddError(
                        result,
                        rowNumber,
                        "Name is required.");

                    rowHasErrors = true;
                }
                else if (name.Length > 200)
                {
                    AddError(
                        result,
                        rowNumber,
                        "Name cannot exceed 200 characters.");

                    rowHasErrors = true;
                }

                if (!TryParseDate(
                        dateOfBirthRaw,
                        out var dateOfBirth))
                {
                    AddError(
                        result,
                        rowNumber,
                        "Date of birth must use yyyy-MM-dd or dd.MM.yyyy format.");

                    rowHasErrors = true;
                }
                else if (dateOfBirth.Date > DateTime.Today)
                {
                    AddError(
                        result,
                        rowNumber,
                        "Date of birth cannot be in the future.");

                    rowHasErrors = true;
                }

                if (!TryParseBoolean(
                        marriedRaw,
                        out var married))
                {
                    AddError(
                        result,
                        rowNumber,
                        "Married must be true/false, yes/no, or 1/0.");

                    rowHasErrors = true;
                }

                if (string.IsNullOrWhiteSpace(phone))
                {
                    AddError(
                        result,
                        rowNumber,
                        "Phone is required.");

                    rowHasErrors = true;
                }
                else if (phone.Length > 50)
                {
                    AddError(
                        result,
                        rowNumber,
                        "Phone cannot exceed 50 characters.");

                    rowHasErrors = true;
                }

                if (!TryParseSalary(
                        salaryRaw,
                        out var salary))
                {
                    AddError(
                        result,
                        rowNumber,
                        "Salary must be a valid decimal number.");

                    rowHasErrors = true;
                }
                else if (salary < 0)
                {
                    AddError(
                        result,
                        rowNumber,
                        "Salary cannot be negative.");

                    rowHasErrors = true;
                }
                else if (salary > MaxSalary)
                {
                    AddError(
                        result,
                        rowNumber,
                        "Salary exceeds the supported database range.");

                    rowHasErrors = true;
                }
                else if (decimal.Round(salary, 2) != salary)
                {
                    AddError(
                        result,
                        rowNumber,
                        "Salary can contain at most 2 decimal places.");

                    rowHasErrors = true;
                }

                if (rowHasErrors)
                {
                    continue;
                }

                result.Contacts.Add(new Contact
                {
                    Name = name!,
                    DateOfBirth = dateOfBirth,
                    Married = married,
                    Phone = phone!,
                    Salary = salary
                });
            }
        }
        catch (CsvHelperException)
        {
            result.Errors.Add(new CsvImportError
            {
                RowNumber =
                    csv.Context.Parser?.Row ?? 1,
                Message =
                    "The CSV file has an invalid structure."
            });
        }

        return result;
    }

    private static bool TryParseDate(
        string? value,
        out DateTime result)
    {
        return DateTime.TryParseExact(
            value,
            SupportedDateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    private static bool TryParseBoolean(
        string? value,
        out bool result)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "true":
            case "yes":
            case "1":
                result = true;
                return true;

            case "false":
            case "no":
            case "0":
                result = false;
                return true;

            default:
                result = false;
                return false;
        }
    }

    private static bool TryParseSalary(
        string? value,
        out decimal result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();

        if (normalized.Contains(',') &&
            !normalized.Contains('.'))
        {
            return decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("uk-UA"),
                out result);
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static void AddError(
        CsvImportResult result,
        int rowNumber,
        string message)
    {
        result.Errors.Add(new CsvImportError
        {
            RowNumber = rowNumber,
            Message = message
        });
    }
}
