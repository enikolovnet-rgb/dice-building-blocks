using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.BuildingBlocks.Tests.Domain;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.NotFound("Sample.NotFound", "Sample was not found.");

    [Fact]
    public void Success_NoValue_IsSuccessAndNotFailure()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void Error_OnSuccess_ThrowsInvalidOperationException()
    {
        var result = Result.Success();

        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_WithError_IsFailureAndCarriesError()
    {
        var result = Result.Failure(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void Failure_NullError_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
        Should.Throw<ArgumentNullException>(() => Result.Failure<int>(null!));
    }

    [Fact]
    public void ImplicitConversion_FromError_ProducesFailedResult()
    {
        Result result = SampleError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void Match_OnSuccess_CallsSuccessBranch()
    {
        var outcome = Result.Success().Match(() => "ok", error => error.Code);

        outcome.ShouldBe("ok");
    }

    [Fact]
    public void Match_OnFailure_CallsFailureBranchWithError()
    {
        var outcome = Result.Failure(SampleError).Match(() => "ok", error => error.Code);

        outcome.ShouldBe(SampleError.Code);
    }

    [Fact]
    public void SuccessOfT_WithValue_CarriesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Value_OnFailure_ThrowsInvalidOperationException()
    {
        var result = Result.Failure<int>(SampleError);

        var exception = Should.Throw<InvalidOperationException>(() => result.Value);
        exception.Message.ShouldContain(SampleError.Code);
    }

    [Fact]
    public void ErrorOfT_OnSuccess_ThrowsInvalidOperationException()
    {
        var result = Result.Success("value");

        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void ImplicitConversionOfT_FromValue_ProducesSuccessfulResult()
    {
        Result<string> result = "hello";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void ImplicitConversionOfT_FromError_ProducesFailedResult()
    {
        Result<string> result = SampleError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void ImplicitConversionOfT_FromHandlerReturns_ProducesMatchingOutcome()
    {
        static Result<int> Parse(string input) =>
            int.TryParse(input, out var number) ? number : Error.Validation("Input.NotNumber", "Not a number.");

        Parse("7").Value.ShouldBe(7);
        Parse("x").Error.Code.ShouldBe("Input.NotNumber");
    }

    [Fact]
    public void MatchOfT_OnSuccess_CallsSuccessBranchWithValue()
    {
        var outcome = Result.Success(5).Match(value => value * 2, _ => -1);

        outcome.ShouldBe(10);
    }

    [Fact]
    public void MatchOfT_OnFailure_CallsFailureBranchWithError()
    {
        var outcome = Result.Failure<int>(SampleError).Match(value => value.ToString(), error => error.Code);

        outcome.ShouldBe(SampleError.Code);
    }

    [Fact]
    public void ResultOfT_AsBaseResult_ExposesSameOutcome()
    {
        Result result = Result.Failure<int>(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }
}
