using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.BuildingBlocks.Tests.Domain;

public sealed class ErrorTests
{
    public static TheoryData<Error, ErrorType> Factories => new()
    {
        { Error.Validation("C", "M"), ErrorType.Validation },
        { Error.NotFound("C", "M"), ErrorType.NotFound },
        { Error.Conflict("C", "M"), ErrorType.Conflict },
        { Error.Unauthorized("C", "M"), ErrorType.Unauthorized },
        { Error.Forbidden("C", "M"), ErrorType.Forbidden },
        { Error.Unexpected("C", "M"), ErrorType.Unexpected },
    };

    [Theory]
    [MemberData(nameof(Factories))]
    public void Factory_WithCodeAndMessage_SetsMatchingType(Error error, ErrorType expectedType)
    {
        error.Type.ShouldBe(expectedType);
        error.Code.ShouldBe("C");
        error.Message.ShouldBe("M");
        error.Details.ShouldBeEmpty();
    }

    [Fact]
    public void Unexpected_NoArguments_UsesDefaultCodeAndMessage()
    {
        var error = Error.Unexpected();

        error.Code.ShouldBe("General.Unexpected");
        error.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Validation_WithDetails_KeepsDetails()
    {
        var details = new Dictionary<string, string[]> { ["Name"] = ["Required."] };

        var error = Error.Validation("Request.Invalid", "Invalid.", details);

        error.Details["Name"].ShouldBe(["Required."]);
    }

    [Theory]
    [InlineData("", "M")]
    [InlineData(" ", "M")]
    [InlineData("C", "")]
    public void Constructor_BlankCodeOrMessage_ThrowsArgumentException(string code, string message)
    {
        Should.Throw<ArgumentException>(() => new Error(code, message, ErrorType.Validation));
    }

    [Fact]
    public void Equals_SameValuesAndEquivalentDetails_ReturnsTrue()
    {
        var left = Error.Validation("C", "M", new Dictionary<string, string[]> { ["A"] = ["x", "y"] });
        var right = Error.Validation("C", "M", new Dictionary<string, string[]> { ["A"] = ["x", "y"] });

        left.ShouldBe(right);
        (left == right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentDetails_ReturnsFalse()
    {
        var left = Error.Validation("C", "M", new Dictionary<string, string[]> { ["A"] = ["x"] });
        var right = Error.Validation("C", "M", new Dictionary<string, string[]> { ["A"] = ["y"] });

        left.ShouldNotBe(right);
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        Error.NotFound("C", "M").ShouldNotBe(Error.Conflict("C", "M"));
    }
}
