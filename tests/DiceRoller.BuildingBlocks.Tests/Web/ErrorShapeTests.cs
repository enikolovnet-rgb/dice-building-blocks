using System.Net.Http.Json;
using System.Text;
using DiceRoller.BuildingBlocks.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DiceRoller.BuildingBlocks.Tests.Web;

/// <summary>The contract of the Web package: every kind of failure produces a body with the same shape.</summary>
public sealed class ErrorShapeTests
{
    private const string FailedResult = "failed Result";
    private const string DomainException = "DomainException";
    private const string ValidationFailure = "validation failure";
    private const string MalformedJsonController = "malformed JSON (controller)";
    private const string MalformedJsonMinimalApi = "malformed JSON (minimal API)";
    private const string UnexpectedException = "unexpected exception";

    private static readonly string[] ExpectedShape =
    [
        "detail:String",
        "errorCode:String",
        "errors:Object",
        "status:Number",
        "title:String",
        "traceId:String",
    ];

    public static TheoryData<string, int> FailureKinds => new()
    {
        { FailedResult, 409 },
        { DomainException, 409 },
        { ValidationFailure, 400 },
        { MalformedJsonController, 400 },
        { MalformedJsonMinimalApi, 400 },
        { UnexpectedException, 500 },
    };

    [Theory]
    [MemberData(nameof(FailureKinds))]
    public async Task FailureKind_AnyKind_ReturnsStandardProblemBody(string kind, int expectedStatus)
    {
        await using var app = await StartAppAsync();

        using var response = await SendAsync(app.GetTestClient(), kind);

        ((int)response.StatusCode).ShouldBe(expectedStatus);
        var problem = await TestApp.ReadProblemAsync(response);
        TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
        TestApp.Shape(problem).ShouldBe(ExpectedShape);
        problem.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
        problem.GetProperty("errorCode").GetString().ShouldNotBeNullOrWhiteSpace();
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AllFailureKinds_SameApp_ProduceIdenticalShape()
    {
        await using var app = await StartAppAsync();
        var client = app.GetTestClient();

        using var reference = await SendAsync(client, FailedResult);
        var referenceShape = TestApp.Shape(await TestApp.ReadProblemAsync(reference));

        foreach (var kind in FailureKinds.Select(row => row.Data.Item1))
        {
            using var response = await SendAsync(client, kind);
            var shape = TestApp.Shape(await TestApp.ReadProblemAsync(response));
            shape.ShouldBe(referenceShape, $"The body of a {kind} differs from the body of a {FailedResult}.");
        }
    }

    private static Task<WebApplication> StartAppAsync() => TestApp.StartAsync(
        builder => builder.Services.AddScoped<IValidator<Order>, OrderValidator>(),
        app => app.MapPost("/minimal", (Order order) => Results.Ok(order)));

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string kind) => kind switch
    {
        FailedResult => client.GetAsync($"/test/failure/{ErrorType.Conflict}", TestApp.Ct),
        DomainException => client.GetAsync("/test/domain", TestApp.Ct),
        ValidationFailure => client.PostAsJsonAsync("/test/orders", new Order(null, 1), TestApp.Ct),
        MalformedJsonController => client.PostAsync("/test/orders", Json("{ \"name\": "), TestApp.Ct),
        MalformedJsonMinimalApi => client.PostAsync("/minimal", Json("{ bad"), TestApp.Ct),
        UnexpectedException => client.GetAsync("/test/unexpected", TestApp.Ct),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown failure kind."),
    };

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");
}
