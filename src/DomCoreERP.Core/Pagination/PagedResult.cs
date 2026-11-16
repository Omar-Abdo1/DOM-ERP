namespace DomCoreERP.Core.Pagination;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static PagedResult<T> Empty(int pageIndex, int pageSize) => new()
    {
        Items = new(),
        PageIndex = pageIndex,
        PageSize = pageSize,
        TotalCount = 0
    };
}