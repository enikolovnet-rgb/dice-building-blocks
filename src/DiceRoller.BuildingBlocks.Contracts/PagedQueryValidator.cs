using FluentValidation;

namespace DiceRoller.BuildingBlocks.Contracts;

/// <summary>
/// Validates a <see cref="PagedQuery"/>: <see cref="PagedQuery.Page"/> must be at least 1 and
/// <see cref="PagedQuery.PageSize"/> must be between 1 and <see cref="PagedQuery.MaxPageSize"/>.
/// </summary>
public sealed class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    /// <summary>Error code reported when <see cref="PagedQuery.Page"/> is less than 1.</summary>
    public const string InvalidPageCode = "Paging.InvalidPage";

    /// <summary>Error code reported when <see cref="PagedQuery.PageSize"/> is outside 1 to <see cref="PagedQuery.MaxPageSize"/>.</summary>
    public const string InvalidPageSizeCode = "Paging.InvalidPageSize";

    /// <summary>Creates the validator.</summary>
    public PagedQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(InvalidPageCode)
            .WithMessage("Page must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PagedQuery.MaxPageSize)
            .WithErrorCode(InvalidPageSizeCode)
            .WithMessage($"Page size must be between 1 and {PagedQuery.MaxPageSize}.");
    }
}
