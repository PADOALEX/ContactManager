using System.ComponentModel.DataAnnotations;
using PADO.ContactFlow.Web.Models;

namespace PADO.ContactFlow.Tests;

public sealed class ValidationTests
{
    [Theory]
    [InlineData("", "+123", "Name")]
    [InlineData("Alice", "   ", "Phone")]
    public void RequiredFields_RejectBlankStrings(string name, string phone, string property)
    {
        var input = Valid(); input.Name = name; input.Phone = phone;
        var errors = Validate(input);
        Assert.Contains(errors, e => e.MemberNames.Contains(property));
    }

    [Fact]
    public void InvalidSalaryAndFutureDate_AreRejected()
    {
        var input = Valid(); input.DateOfBirth = DateTime.Today.AddDays(1); input.Salary = 5.555m;
        var errors = Validate(input);
        Assert.Contains(errors, e => e.MemberNames.Contains("DateOfBirth"));
        Assert.Contains(errors, e => e.MemberNames.Contains("Salary"));
    }

    [Fact]
    public void ValidFields_AreTrimmedBeforeSaving()
    {
        var input = Valid(); input.Name = "  Alice  "; input.Phone = "  +123  ";
        Assert.Empty(Validate(input));
        var mapped = input.ToContact();
        Assert.Equal("Alice", mapped.Name);
        Assert.Equal("+123", mapped.Phone);
    }

    [Fact]
    public void QueryNormalization_ClampsAndWhitelists()
    {
        var query = new ContactQueryParameters
        {
            Page = -1, PageSize = 500, Search = new string('x', 120),
            SortBy = (ContactSortColumn)999, SortDirection = (SortDirection)999,
            FilterBy = (ContactFilterColumn)999
        };
        query.Normalize();
        Assert.Equal(1, query.Page);
        Assert.Equal(100, query.PageSize);
        Assert.Equal(100, query.Search!.Length);
        Assert.Equal(ContactSortColumn.Id, query.SortBy);
        Assert.Equal(ContactFilterColumn.All, query.FilterBy);
    }

    private static ContactCreateRequest Valid() => new()
    {
        Name = "Alice", DateOfBirth = new DateTime(2000, 1, 1),
        Married = false, Phone = "+123", Salary = 1.20m
    };

    private static List<ValidationResult> Validate(ContactCreateRequest input)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), errors, true);
        return errors;
    }
}
