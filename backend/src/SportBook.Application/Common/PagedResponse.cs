namespace SportBook.Application.Common;

/// <summary>
/// Envelope for every list endpoint (contracts/api.md pagination contract): offset pagination
/// with a 1-based page number. Shared so all list responses stay wire-identical.
/// </summary>
public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>
/// Normalized page/pageSize query input: page is 1-based, pageSize defaults to 20 and is capped
/// at 100 (research.md pagination decision) so a client cannot request unbounded result sets.
/// </summary>
public record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private readonly int _page = 1;
    private readonly int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }

    /// <summary>Rows to skip for the current page.</summary>
    public int Skip => (Page - 1) * PageSize;
}
