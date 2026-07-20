namespace SportBook.Application.Common;

/// <summary>
/// Envelope for every list endpoint (contracts/api.md pagination contract): offset pagination
/// with a 1-based page number. Shared so all list responses stay wire-identical.
/// </summary>
public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>
/// Normalized page/pageSize query input: page is 1-based, pageSize defaults to 20 and is capped
/// at 100 (research.md pagination decision) so a client cannot request unbounded result sets.
/// A positional record with default constructor parameters (not a property-only record) so
/// Minimal API's `[AsParameters]` binder sees `page`/`pageSize` as optional query parameters,
/// matching MVC's prior `[FromQuery]` complex-object binding where an omitted value fell through
/// to the default - a property-only record's `init` defaults are invisible to that binder and
/// make both query params required instead (consilium 2026-07-20 Minimal API migration).
/// </summary>
public record PageRequest(int Page = 1, int PageSize = PageRequest.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; init; } = Page < 1 ? 1 : Page;

    public int PageSize { get; init; } = PageSize < 1 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize);

    /// <summary>Rows to skip for the current page.</summary>
    public int Skip => (Page - 1) * PageSize;
}
