using System.Text.Json;
using DiceRoller.BuildingBlocks.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace DiceRoller.BuildingBlocks.Tests.Web;

/// <summary>A minimal service host built with the shared defaults, running on <see cref="TestServer"/>.</summary>
internal static class TestApp
{
    public static readonly string[] ErrorBodyProperties = ["status", "title", "detail", "errorCode", "errors", "traceId"];

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task<WebApplication> StartAsync(
        Action<WebApplicationBuilder>? configure = null,
        Action<WebApplication>? map = null,
        string environment = "Production")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
            ApplicationName = typeof(TestApp).Assembly.GetName().Name,
        });
        builder.WebHost.UseTestServer();
        builder.AddServiceDefaults();
        configure?.Invoke(builder);

        var app = builder.Build();
        app.UseServiceDefaults();
        map?.Invoke(app);

        await app.StartAsync(Ct);
        return app;
    }

    public static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ErrorMapper.ContentType);
        var json = await response.Content.ReadAsStringAsync(Ct);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    public static string[] PropertyNames(JsonElement element) =>
        [.. element.EnumerateObject().Select(property => property.Name)];

    public static Dictionary<string, string[]> Errors(JsonElement problem) =>
        problem.GetProperty("errors").EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.EnumerateArray().Select(message => message.GetString()!).ToArray());
}
