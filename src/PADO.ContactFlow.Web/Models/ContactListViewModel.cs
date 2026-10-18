namespace PADO.ContactFlow.Web.Models;

public sealed class ContactListViewModel
{
    public required PagedResult<Contact> Result { get; init; }

    public required ContactQueryParameters Query { get; init; }
}