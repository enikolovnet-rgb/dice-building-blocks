using DiceRoller.BuildingBlocks.Contracts;
using FluentValidation.TestHelper;

namespace DiceRoller.BuildingBlocks.Tests.Contracts;

public sealed class PagedQueryValidatorTests
{
    private readonly PagedQueryValidator _validator = new();

    [Fact]
    public void Validate_Defaults_IsValid()
    {
        var query = new PagedQuery();

        query.Page.ShouldBe(1);
        query.PageSize.ShouldBe(10);
        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 100)]
    [InlineData(500, 50)]
    public void Validate_BoundaryValues_IsValid(int page, int pageSize)
    {
        var result = _validator.TestValidate(new PagedQuery { Page = page, PageSize = pageSize });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageBelowOne_FailsWithInvalidPageCode(int page)
    {
        var result = _validator.TestValidate(new PagedQuery { Page = page });

        result.ShouldHaveValidationErrorFor(query => query.Page)
            .WithErrorCode(PagedQueryValidator.InvalidPageCode);
        result.ShouldNotHaveValidationErrorFor(query => query.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutOfRange_FailsWithInvalidPageSizeCode(int pageSize)
    {
        var result = _validator.TestValidate(new PagedQuery { PageSize = pageSize });

        result.ShouldHaveValidationErrorFor(query => query.PageSize)
            .WithErrorCode(PagedQueryValidator.InvalidPageSizeCode);
        result.ShouldNotHaveValidationErrorFor(query => query.Page);
    }
}
