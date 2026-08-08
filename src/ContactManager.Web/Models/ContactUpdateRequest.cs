using System.ComponentModel.DataAnnotations;

namespace ContactManager.Web.Models;

public sealed class ContactUpdateRequest
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime? DateOfBirth { get; set; }

    public bool Married { get; set; }

    [Required]
    [StringLength(50)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public decimal? Salary { get; set; }
}
