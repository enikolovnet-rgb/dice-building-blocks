using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;

namespace DiceRoller.BuildingBlocks.Tests.Web;

public sealed class ResultExtensionsTests
{
    [Fact]
    public async Task ToActionResult_SuccessWithValue_Returns200WithValue()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().GetAsync("/test/value", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var item = await response.Content.ReadFromJsonAsync<Item>(TestApp.Ct);
        item.ShouldBe(new Item(1, "dice"));
    }

    [Fact]
    public async Task ToActionResult_FailureOfT_ReturnsMappedError()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().GetAsync("/test/value-failure", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await TestApp.ReadProblemAsync(response);
        problem.GetProperty("errorCode").GetString().ShouldBe("Item.NotFound");
    }

    [Fact]
    public async Task ToActionResult_SuccessWithoutValue_Returns204()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().DeleteAsync("/test/value", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ToCreatedResult_SuccessWithValue_Returns201WithLocationAndValue()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().PostAsync("/test/items", content: null, TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.ToString().ShouldEndWith("/test/items/5");
        var item = await response.Content.ReadFromJsonAsync<Item>(TestApp.Ct);
        item.ShouldBe(new Item(5, "dice"));
    }

    [Fact]
    public async Task ToCreatedResult_SuccessWithoutValue_Returns201WithLocation()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().PostAsync("/test/items-no-body", content: null, TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.ToString().ShouldEndWith("/test/items/7");
    }

    [Fact]
    public async Task ToCreatedResult_Failure_ReturnsMappedError()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().PostAsync("/test/items-conflict", content: null, TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await TestApp.ReadProblemAsync(response);
        problem.GetProperty("errorCode").GetString().ShouldBe("Item.Exists");
    }
}
