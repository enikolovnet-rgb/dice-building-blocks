using DiceRoller.BuildingBlocks.Contracts;

namespace DiceRoller.BuildingBlocks.Tests.Contracts;

public sealed class PagedResponseTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(21, 10, 3)]
    [InlineData(5, 0, 0)]
    public void TotalPages_ForCountAndPageSize_RoundsUp(int totalCount, int pageSize, int expectedPages)
    {
        var response = new PagedResponse<int>([], 1, pageSize, totalCount);

        response.TotalPages.ShouldBe(expectedPages);
    }
}
