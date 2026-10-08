using System.Net;
using System.Text.Json;
using DiceRoller.BuildingBlocks.Web;
using Microsoft.AspNetCore.TestHost;

namespace DiceRoller.BuildingBlocks.Tests.Web;

public sealed class ServiceDefaultsTests
{
    [Fact]
    public async Task CorrelationId_SentByClient_IsEchoedOnResponse()
    {
        await using var app = await TestApp.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/value");
        request.Headers.Add(ServiceDefaultsExtensions.CorrelationIdHeader, "abc-123");

        var response = await app.GetTestClient().SendAsync(request, TestApp.Ct);

        response.Headers.GetValues(ServiceDefaultsExtensions.CorrelationIdHeader).ShouldBe(["abc-123"]);
    }

    [Fact]
    public async Task CorrelationId_Missing_IsGenerated()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().GetAsync("/test/value", TestApp.Ct);

        response.Headers.GetValues(ServiceDefaultsExtensions.CorrelationIdHeader).Single().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CorrelationId_OnErrorResponse_IsStillReturned()
    {
        await using var app = await TestApp.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/unexpected");
        request.Headers.Add(ServiceDefaultsExtensions.CorrelationIdHeader, "err-1");

        var response = await app.GetTestClient().SendAsync(request, TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Headers.GetValues(ServiceDefaultsExtensions.CorrelationIdHeader).ShouldBe(["err-1"]);
    }

    [Fact]
    public async Task CorrelationId_Unsafe_IsReplaced()
    {
        await using var app = await TestApp.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/value");
        request.Headers.TryAddWithoutValidation(ServiceDefaultsExtensions.CorrelationIdHeader, "bad value <script>");

        var response = await app.GetTestClient().SendAsync(request, TestApp.Ct);

        response.Headers.GetValues(ServiceDefaultsExtensions.CorrelationIdHeader).Single().ShouldNotBe("bad value <script>");
    }

    [Fact]
    public async Task OpenApi_InDevelopment_DescribesBearerSecurityScheme()
    {
        await using var app = await TestApp.StartAsync(environment: "Development");

        var response = await app.GetTestClient().GetAsync("/openapi/v1.json", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestApp.Ct));
        var scheme = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        scheme.GetProperty("scheme").GetString().ShouldBe("bearer");
    }

    [Fact]
    public async Task OpenApi_InProduction_IsNotMapped()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetTestClient().GetAsync("/openapi/v1.json", TestApp.Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
