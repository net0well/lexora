namespace Lexora.Application.Common.Paging;

public static class PagedResponse
{
    public static PagedResponse<T> Create<T>(IReadOnlyList<T> items, PageRequest request, int totalItems) => new(
        items,
        request.Page,
        request.PageSize,
        totalItems,
        (int)Math.Ceiling(totalItems / (double)request.PageSize));
}
