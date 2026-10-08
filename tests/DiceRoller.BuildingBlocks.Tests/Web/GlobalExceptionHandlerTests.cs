using System.Net;
using DiceRoller.BuildingBlocks.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace DiceRoller.BuildingBlocks.Tests.Web;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandle_DomainException_ReturnsMappedErrorOfException()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().GetAsync("/test/domain", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await TestApp.ReadProblemAsync(response);
        problem.GetProperty("errorCode").GetString().ShouldBe("Test.Conflict");
        problem.GetProperty("detail").GetString().ShouldBe("Domain rule broken.");
    }

    [Fact]
    public async Task TryHandle_UnexpectedExceptionInProduction_Returns500WithoutDetails()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().GetAsync("/test/unexpected", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var problem = await TestApp.ReadProblemAsync(response);
        TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
        problem.GetProperty("errorCode").GetString().ShouldBe("General.Unexpected");
        problem.GetRawText().ShouldNotContain(TestController.SecretMessage);
    }

    [Fact]
    public async Task TryHandle_UnexpectedExceptionInDevelopment_IncludesExceptionDetails()
    {
        await using var app = await TestApp.StartAsync(environment: "Development");

        var response = await app.GetTestClient().GetAsync("/test/unexpected", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var problem = await TestApp.ReadProblemAsync(response);
        var exception = problem.GetProperty(GlobalExceptionHandler.ExceptionKey);
        exception.GetProperty("message").GetString().ShouldBe(TestController.SecretMessage);
    }

    [Fact]
    public async Task TryHandle_BadHttpRequestException_Returns400Validation()
    {
        await using var app = await TestApp.StartAsync(map: app =>
            app.MapGet("/bad", IResult () => throw new BadHttpRequestException("Unreadable.")));

        var response = await app.GetTestClient().GetAsync("/bad", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await TestApp.ReadProblemAsync(response);
        TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
        problem.GetProperty("errorCode").GetString().ShouldBe(GlobalExceptionHandler.BadRequestCode);
    }

    [Fact]
    public async Task TryHandle_MinimalApiMalformedJson_Returns400InStandardShape()
    {
        await using var app = await TestApp.StartAsync(map: app =>
            app.MapPost("/minimal", (Order order) => Results.Ok(order)));
        using var content = new StringContent("{ bad", System.Text.Encoding.UTF8, "application/json");

        var response = await app.GetTestClient().PostAsync("/minimal", content, TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await TestApp.ReadProblemAsync(response);
        problem.GetProperty("errorCode").GetString().ShouldBe(GlobalExceptionHandler.BadRequestCode);
    }
}
