using System.Text;
using Microsoft.AspNetCore.Http;
using PADO.ContactFlow.Web.Services;

namespace PADO.ContactFlow.Tests;

public sealed class CsvImportTests
{
    private static readonly CsvImportService Service = new();
    private const string Header = "Name,Date of birth,Married,Phone,Salary\n";
    private const string Valid = "Alice,2000-01-01,yes,+380501000001,1050.50\n";

    [Fact]
    public async Task ValidUtf8BomFile_ImportsUnicodeFields()
    {
        var bytes = new UTF8Encoding(true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(Header + "Ірина,2000-01-01,yes,+380501000001,1050.50\n")).ToArray();
        var result = await Service.ParseAsync(File(bytes));
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Single(result.Contacts);
        Assert.Equal("Ірина", result.Contacts[0].Name);
        Assert.Equal(1, result.Rows[0].LineNumber - 1);
    }

    [Fact]
    public async Task InvalidUtf8Bytes_AreRejected()
    {
        var result = await Service.ParseAsync(File(Encoding.UTF8.GetBytes(Header).Concat(new byte[] { 0xC3, 0x28 }).ToArray()));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("UTF-8"));
    }

    [Theory]
    [InlineData("Name,Date of birth,Married,Phone,Phone\n", "Headers")]
    [InlineData("Name,Date of birth,Married,Phone\n", "Headers")]
    [InlineData("Name,Date of birth,Married,Phone,Salary,Extra\n", "Headers")]
    public async Task InvalidHeaders_AreRejected(string data, string error)
    {
        var result = await Service.ParseAsync(File(Encoding.UTF8.GetBytes(data)));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains(error));
    }

    [Fact]
    public async Task ReorderedCaseInsensitiveHeaders_AreAccepted()
    {
        var text = "PHONE,married,SALARY,NAME,DATE OF BIRTH\n+380501000001,no,0,Alice,2000-01-01\n";
        var result = await Service.ParseAsync(File(Encoding.UTF8.GetBytes(text)));
        Assert.True(result.IsValid);
        Assert.Equal("Alice", Assert.Single(result.Contacts).Name);
    }

    [Fact]
    public async Task DuplicatePhoneInsideFile_ReportsPhysicalLine()
    {
        var text = Header + Valid + "Bob,1998-02-02,no,+380501000001,10.00\n";
        var result = await Service.ParseAsync(File(Encoding.UTF8.GetBytes(text)));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RowNumber == 3 && e.Message.Contains("Duplicate"));
        Assert.Equal(2, result.DataRowsRead);
    }

    [Fact]
    public async Task QuotedUkrainianDecimalComma_RemainsSupported()
    {
        var csv = Header + "Alice,2000-01-01,yes,+321,\"125,50\"\n";
        var result = await Service.ParseAsync(File(Encoding.UTF8.GetBytes(csv)));
        Assert.True(result.IsValid);
        Assert.Equal(125.50m, Assert.Single(result.Contacts).Salary);
    }

    [Fact]
    public async Task InvalidFields_ReportLineSpecificErrors()
    {
        var text = Header + "  ,2099-01-01,maybe,, -12.345\n";
        var result = await Service.ParseAsync(File(Encoding.UTF8.GetBytes(text)));
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4);
        Assert.All(result.Errors, e => Assert.Equal(2, e.RowNumber));
    }

    [Fact]
    public async Task BrokenColumnCount_IsRejected()
    {
        var result = await Service.ParseAsync(File(Encoding.UTF8.GetBytes(Header + "Alice,2000-01-01,true,+123\n")));
        Assert.Contains(result.Errors, e => e.Message.Contains("Expected 5 fields"));
    }

    [Fact]
    public async Task EmptyCsv_IsRejected()
    {
        var result = await Service.ParseAsync(File([]));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task QuotedCommaInName_IsParsedCorrectly()
    {
        var result = await Service.ParseAsync(File(Encoding.UTF8.GetBytes(Header + "\"Doe, Jane\",2001-01-01,true,+987,50.25\n")));
        Assert.True(result.IsValid);
        Assert.Equal("Doe, Jane", Assert.Single(result.Contacts).Name);
    }

    private static FormFile File(byte[] bytes) =>
        new(new MemoryStream(bytes), 0, bytes.Length, "file", "test.csv");
}
