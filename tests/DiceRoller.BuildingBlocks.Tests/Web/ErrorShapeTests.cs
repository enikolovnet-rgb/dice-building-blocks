using System.Net.Http.Json;
using DiceRoller.BuildingBlocks.Domain;
using FluentValidation;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DiceRoller.BuildingBlocks.Tests.Web;

/// <summary>The contract of the Web package: every kind of failure produces a body with the same shape.</summary>
public sealed class ErrorShapeTests
{
    [Fact]
    public async Task AllFailureKinds_ProduceBodiesWithTheSameShape()
    {
        await using var app = await TestApp.StartAsync(builder =>
            builder.Services.AddScoped<IValidator<Order>, OrderValidator>());
        var client = app.GetTestClient();

        HttpResponseMessage[] responses =
        [
            await client.GetAsync($"/test/failure/{ErrorType.Conflict}", TestApp.Ct),
            await client.GetAsync("/test/domain", TestApp.Ct),
            await client.PostAsJsonAsync("/test/orders", new Order(null, 1), TestApp.Ct),
            await client.GetAsync("/test/unexpected", TestApp.Ct),
        ];

        foreach (var response in responses)
        {
            var problem = await TestApp.ReadProblemAsync(response);
            TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
            problem.GetProperty("status").GetInt32().ShouldBe((int)response.StatusCode);
        }
    }
}
