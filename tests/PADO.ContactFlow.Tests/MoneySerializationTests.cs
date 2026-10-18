using System.Text.Json;
using PADO.ContactFlow.Web.Models;

namespace PADO.ContactFlow.Tests;

public sealed class MoneySerializationTests
{
    [Theory]
    [InlineData("1234567890123456.78")]
    [InlineData("9999999999999999.99")]
    [InlineData("0.01")]
    public void DecimalString_IsDeserializedExactly(string jsonSalary)
    {
        var json = $$"""{"name":"Alice","dateOfBirth":"2000-01-01","married":false,"phone":"+123","salary":"{{jsonSalary}}"}""";
        var request = JsonSerializer.Deserialize<ContactCreateRequest>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(request);
        Assert.Equal(decimal.Parse(jsonSalary, System.Globalization.CultureInfo.InvariantCulture), request.Salary);
    }

    [Fact]
    public void InvalidDecimalString_IsRejectedByJsonDeserializer()
    {
        var json = """{"name":"Alice","dateOfBirth":"2000-01-01","married":false,"phone":"+123","salary":"not money"}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ContactCreateRequest>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
