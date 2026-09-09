namespace BuildingBlocks.Contracts.Dtos;

/// <summary>
/// Wraps a paginated list with metadata: total count, current page, page size, and has-next-page indicator.
/// Enables clients to build smart pagination UI (prev/next buttons, jump-to-page, etc).
/// </summary>
public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasNextPage)
{
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>
/// Factory for creating PaginatedResponse instances. Extracted to a non-generic class to avoid CA1000.
/// </summary>
public static class PaginatedResponseFactory
{
    public static PaginatedResponse<T> Criar<T>(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize) =>
        new(items, totalCount, pageNumber, pageSize, HasNextPageIndicator(totalCount, pageNumber, pageSize));

    private static bool HasNextPageIndicator(int totalCount, int pageNumber, int pageSize) =>
        pageNumber * pageSize < totalCount;
}
