namespace PADO.ContactFlow.Web.Models;

public sealed class ContactQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ContactSortColumn SortBy { get; set; } = ContactSortColumn.Id;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
    public string? Search { get; set; }
    public ContactFilterColumn FilterBy { get; set; } = ContactFilterColumn.All;

    public void Normalize()
    {
        Page = Math.Max(Page, 1);
        PageSize = Math.Clamp(PageSize, 1, 100);
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        if (Search is { Length: > 100 }) Search = Search[..100];
        if (!Enum.IsDefined(SortBy)) SortBy = ContactSortColumn.Id;
        if (!Enum.IsDefined(SortDirection)) SortDirection = SortDirection.Desc;
        if (!Enum.IsDefined(FilterBy)) FilterBy = ContactFilterColumn.All;
    }
}
