namespace ContactManager.Web.Models;

public sealed class ContactQueryParameters
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public ContactSortColumn SortBy { get; set; } = ContactSortColumn.Id;

    public SortDirection SortDirection { get; set; } = SortDirection.Desc;

    public void Normalize()
    {
        Page = Math.Max(Page, 1);
        PageSize = Math.Clamp(PageSize, 1, 100);
    }
}