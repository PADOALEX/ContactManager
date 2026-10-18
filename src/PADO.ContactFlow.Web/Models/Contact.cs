namespace PADO.ContactFlow.Web.Models;

public sealed class Contact
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public bool Married { get; set; }

    public string Phone { get; set; } = string.Empty;

    public decimal Salary { get; set; }
}