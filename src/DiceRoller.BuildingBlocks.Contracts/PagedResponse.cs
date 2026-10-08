namespace DiceRoller.BuildingBlocks.Contracts;

/// <summary>
/// One page of a larger result set.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="Page">The 1-based page number.</param>
/// <param name="PageSize">The maximum number of items per page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>The total number of pages. Zero when there are no items or <see cref="PageSize"/> is not positive.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}
