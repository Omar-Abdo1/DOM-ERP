public class PaginationArg
{
    const int MaxPageSize = 100;
    
    private int _pageSize = 20;
    private int _pageIndex = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(MaxPageSize, Math.Max(1, value));
    }

    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = Math.Max(1, value);
    }
}