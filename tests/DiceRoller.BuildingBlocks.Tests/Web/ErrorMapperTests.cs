using System.Net;
using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.BuildingBlocks.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace DiceRoller.BuildingBlocks.Tests.Web;

public sealed class ErrorMapperTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400, "Bad Request")]
    [InlineData(ErrorType.Unauthorized, 401, "Unauthorized")]
    [InlineData(ErrorType.Forbidden, 403, "Forbidden")]
    [InlineData(ErrorType.NotFound, 404, "Not Found")]
    [InlineData(ErrorType.Conflict, 409, "Conflict")]
    [InlineData(ErrorType.Unexpected, 500, "Internal Server Error")]
    public async Task ToActionResult_EachErrorType_WritesStatusAndStandardBody(ErrorType type, int status, string title)
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().GetAsync($"/test/failure/{type}", TestApp.Ct);

        ((int)response.StatusCode).ShouldBe(status);
        var problem = await TestApp.ReadProblemAsync(response);
        TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
        problem.GetProperty("status").GetInt32().ShouldBe(status);
        problem.GetProperty("title").GetString().ShouldBe(title);
        problem.GetProperty("detail").GetString().ShouldBe(TestController.FailureMessage);
        problem.GetProperty("errorCode").GetString().ShouldBe("Test.Failed");
        TestApp.Errors(problem).ShouldBeEmpty();
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unexpected, 500)]
    public void ToStatusCode_EachErrorType_ReturnsMappedStatus(ErrorType type, int status)
    {
        ErrorMapper.ToStatusCode(type).ShouldBe(status);
    }

    [Fact]
    public async Task WriteAsync_ValidationErrorWithDetails_WritesDetailsAsErrors()
    {
        var details = new Dictionary<string, string[]> { ["Sides"] = ["Too few.", "Must be even."] };
        await using var app = await TestApp.StartAsync(map: app => app.MapGet(
            "/write",
            (HttpContext context) =>
                ErrorMapper.WriteAsync(context, Error.Validation("Roll.Invalid", "Invalid roll.", details))));

        var response = await app.GetTestClient().GetAsync("/write", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await TestApp.ReadProblemAsync(response);
        TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
        TestApp.Errors(problem)["Sides"].ShouldBe(["Too few.", "Must be even."]);
    }
}
