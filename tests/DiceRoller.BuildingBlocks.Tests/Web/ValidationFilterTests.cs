using System.Net;
using System.Net.Http.Json;
using System.Text;
using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Web;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DiceRoller.BuildingBlocks.Tests.Web;

public sealed class ValidationFilterTests
{
    private static void AddValidators(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IValidator<Order>, OrderValidator>();
        builder.Services.AddScoped<IValidator<Order>, OrderLimitValidator>();
        builder.Services.AddScoped<IValidator<PagedQuery>, PagedQueryValidator>();
    }

    [Fact]
    public async Task OnActionExecution_FailuresOnSeveralProperties_Returns400WithAllMessagesPerField()
    {
        await using var app = await TestApp.StartAsync(AddValidators);

        var response = await app.GetTestClient().PostAsJsonAsync("/test/orders", new Order("", 5001), TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await TestApp.ReadProblemAsync(response);
        TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
        problem.GetProperty("errorCode").GetString().ShouldBe(ValidationFilter.ErrorCode);
        problem.GetProperty("detail").GetString().ShouldBe(ValidationFilter.ErrorMessage);

        var errors = TestApp.Errors(problem);
        errors.Keys.ShouldBe(["Name", "Quantity"], ignoreOrder: true);
        errors["Name"].ShouldBe(["Name is required.", "Name must be at least 3 characters."], ignoreOrder: true);
        errors["Quantity"].ShouldBe(["Quantity must be even.", "Quantity must be less than 1000."], ignoreOrder: true);
    }

    [Fact]
    public async Task OnActionExecution_ValidRequest_RunsAction()
    {
        await using var app = await TestApp.StartAsync(AddValidators);

        var response = await app.GetTestClient().PostAsJsonAsync("/test/orders", new Order("dice", 2), TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OnActionExecution_InvalidPagedQuery_Returns400WithPagingErrors()
    {
        await using var app = await TestApp.StartAsync(AddValidators);

        var response = await app.GetTestClient().GetAsync("/test/paged?page=0&pageSize=500", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = TestApp.Errors(await TestApp.ReadProblemAsync(response));
        errors.Keys.ShouldBe(["Page", "PageSize"], ignoreOrder: true);
    }

    [Fact]
    public async Task OnActionExecution_MalformedJsonBody_Returns400InStandardShape()
    {
        await using var app = await TestApp.StartAsync(AddValidators);
        using var content = new StringContent("{ \"name\": ", Encoding.UTF8, "application/json");

        var response = await app.GetTestClient().PostAsync("/test/orders", content, TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await TestApp.ReadProblemAsync(response);
        TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
        problem.GetProperty("errorCode").GetString().ShouldBe(ValidationFilter.ErrorCode);
        TestApp.Errors(problem).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task OnActionExecution_WrongJsonType_Returns400InStandardShape()
    {
        await using var app = await TestApp.StartAsync(AddValidators);
        using var content = new StringContent("{ \"name\": \"dice\", \"quantity\": \"many\" }", Encoding.UTF8, "application/json");

        var response = await app.GetTestClient().PostAsync("/test/orders", content, TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await TestApp.ReadProblemAsync(response);
        problem.GetProperty("errorCode").GetString().ShouldBe(ValidationFilter.ErrorCode);
    }
}
