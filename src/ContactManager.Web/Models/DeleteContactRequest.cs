using System.ComponentModel.DataAnnotations;

namespace ContactManager.Web.Models;

public sealed class DeleteContactRequest
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}