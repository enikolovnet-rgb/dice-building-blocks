using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.BuildingBlocks.Tests.Domain;

public sealed class GuardTests
{
    private static readonly Error SampleError = Error.Validation("Sample.Invalid", "Sample is invalid.");

    [Fact]
    public void Against_ConditionTrue_ThrowsDomainExceptionWithError()
    {
        var exception = Should.Throw<DomainException>(() => Guard.Against(true, SampleError));

        exception.Error.ShouldBe(SampleError);
        exception.Message.ShouldBe(SampleError.Message);
    }

    [Fact]
    public void Against_ConditionFalse_DoesNotThrow()
    {
        Should.NotThrow(() => Guard.Against(false, SampleError));
    }

    [Fact]
    public void Against_NullError_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Guard.Against(false, null!));
    }
}
