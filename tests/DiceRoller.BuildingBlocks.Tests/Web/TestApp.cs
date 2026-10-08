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
        response.Content.Headers.ContentType.ShouldNotBeNull();
        response.Content.Headers.ContentType.MediaType.ShouldBe(ErrorMapper.ContentType);
        var json = await response.Content.ReadAsStringAsync(Ct);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    public static string[] PropertyNames(JsonElement element) =>
        [.. element.EnumerateObject().Select(property => property.Name)];

    /// <summary>
    /// The shape of an error body: each property with its JSON kind, sorted by name. <c>errors</c> is reported as
    /// <c>Object</c> only when every value is an array of strings.
    /// </summary>
    public static string[] Shape(JsonElement problem) =>
    [
        .. problem.EnumerateObject()
            .Select(property => $"{property.Name}:{KindOf(property)}")
            .Order(StringComparer.Ordinal),
    ];

    private static string KindOf(JsonProperty property)
    {
        var kind = property.Value.ValueKind;
        var isErrorsMap = property.Name == "errors"
            && kind == JsonValueKind.Object
            && property.Value.EnumerateObject().All(field =>
                field.Value.ValueKind == JsonValueKind.Array
                && field.Value.EnumerateArray().All(message => message.ValueKind == JsonValueKind.String));

        return property.Name == "errors" && !isErrorsMap ? $"invalid {kind}" : kind.ToString();
    }

    public static Dictionary<string, string[]> Errors(JsonElement problem) =>
        problem.GetProperty("errors").EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.EnumerateArray().Select(message => message.GetString()!).ToArray());
}
