using System.ComponentModel.DataAnnotations;

namespace PADO.ContactFlow.Web.Models;

public sealed class ContactUpdateRequest : ContactFieldsRequest
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}
