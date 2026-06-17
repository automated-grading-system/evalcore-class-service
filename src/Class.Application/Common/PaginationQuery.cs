namespace Class.Application.Common;

public sealed class PaginationQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public PageRequest ToPageRequest()
    {
        return PageRequest.Create(Page, PageSize);
    }
}

public readonly record struct PageRequest(int Page, int PageSize)
{
    public int Skip => (Page - 1) * PageSize;

    public static PageRequest Create(int page, int pageSize)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        return new PageRequest(normalizedPage, normalizedPageSize);
    }
}
