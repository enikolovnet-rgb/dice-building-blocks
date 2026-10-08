namespace DiceRoller.BuildingBlocks.Contracts;

/// <summary>
/// Paging parameters of a list request. Validate it with <see cref="PagedQueryValidator"/>.
/// </summary>
public sealed record PagedQuery
{
    /// <summary>The page returned when none is requested.</summary>
    public const int DefaultPage = 1;

    /// <summary>The page size used when none is requested.</summary>
    public const int DefaultPageSize = 10;

    /// <summary>The largest page size a client may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The 1-based page number. Defaults to <see cref="DefaultPage"/>.</summary>
    public int Page { get; init; } = DefaultPage;

    /// <summary>The number of items per page, from 1 to <see cref="MaxPageSize"/>. Defaults to <see cref="DefaultPageSize"/>.</summary>
    public int PageSize { get; init; } = DefaultPageSize;
}
