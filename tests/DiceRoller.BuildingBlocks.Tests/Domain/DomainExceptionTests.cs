using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.BuildingBlocks.Tests.Domain;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithError_UsesErrorMessage()
    {
        var error = Error.Conflict("Sample.Conflict", "Sample already exists.");

        var exception = new DomainException(error);

        exception.Error.ShouldBe(error);
        exception.Message.ShouldBe(error.Message);
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_KeepsInnerException()
    {
        var inner = new InvalidOperationException("inner");

        var exception = new DomainException(Error.Unexpected(), inner);

        exception.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void Constructor_NullError_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new DomainException(null!));
    }
}
