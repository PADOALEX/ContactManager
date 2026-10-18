using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PADO.ContactFlow.Web.Models;

public class ContactFieldsRequest : IValidatableObject
{
    [Required, StringLength(ContactRules.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime? DateOfBirth { get; set; }

    public bool Married { get; set; }

    [Required, StringLength(ContactRules.PhoneMaxLength)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Salary { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Name is required.", [nameof(Name)]);

        if (string.IsNullOrWhiteSpace(Phone))
            yield return new ValidationResult("Phone is required.", [nameof(Phone)]);

        if (DateOfBirth is { } date && date.Date > DateTime.Today)
            yield return new ValidationResult("Date of birth cannot be in the future.", [nameof(DateOfBirth)]);

        if (Salary is { } salary)
        {
            if (salary < 0 || salary > ContactRules.MaxSalary)
                yield return new ValidationResult("Salary must be between 0 and 9999999999999999.99.", [nameof(Salary)]);
            else if (decimal.Round(salary, 2) != salary)
                yield return new ValidationResult("Salary can contain at most 2 decimal places.", [nameof(Salary)]);
        }
    }

    public Contact ToContact(int id = 0) => new()
    {
        Id = id,
        Name = Name.Trim(),
        DateOfBirth = DateOfBirth!.Value.Date,
        Married = Married,
        Phone = Phone.Trim(),
        Salary = Salary!.Value
    };
}
