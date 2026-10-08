using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DiceRoller.BuildingBlocks.Tests.Web;

public sealed class JwtAuthenticationTests
{
    // Test-only key; never used outside this test project.
    private const string SigningKey = "test-only-signing-key-0123456789-abcdef";
    private const string OtherKey = "another-test-only-signing-key-9876543210";
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";

    private static Task<WebApplication> StartAsync(string signingKey = SigningKey) =>
        TestApp.StartAsync(builder =>
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:SigningKey"] = signingKey,
                ["Jwt:ExpiryMinutes"] = "15",
            });
            builder.Services.AddJwtAuthentication(builder.Configuration);
        });

    private static string CreateToken(
        string key = SigningKey,
        string issuer = Issuer,
        string audience = Audience,
        DateTime? expires = null)
    {
        var expiry = expires ?? DateTime.UtcNow.AddMinutes(5);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtClaimNames.Subject, "user-1")]),
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expiry.AddMinutes(-10),
            NotBefore = expiry.AddMinutes(-10),
            Expires = expiry,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256),
        });
    }

    private static async Task<HttpResponseMessage> GetAsync(WebApplication app, string path, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await app.GetTestClient().SendAsync(request, TestApp.Ct);
    }

    [Fact]
    public async Task Authenticate_ValidToken_Returns200WithUnmappedSubClaim()
    {
        await using var app = await StartAsync();

        var response = await GetAsync(app, "/test/me", CreateToken());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(TestApp.Ct);
        body!["subject"].ShouldBe("user-1");
    }

    public static TheoryData<string, string?, string> InvalidTokens => new()
    {
        { "no token", null, JwtAuthenticationExtensions.MissingTokenCode },
        { "expired", CreateToken(expires: DateTime.UtcNow.AddMinutes(-5)), JwtAuthenticationExtensions.ExpiredTokenCode },
        { "wrong key", CreateToken(key: OtherKey), JwtAuthenticationExtensions.InvalidTokenCode },
        { "wrong issuer", CreateToken(issuer: "other-issuer"), JwtAuthenticationExtensions.InvalidTokenCode },
        { "wrong audience", CreateToken(audience: "other-audience"), JwtAuthenticationExtensions.InvalidTokenCode },
        { "not a jwt", "not-a-token", JwtAuthenticationExtensions.InvalidTokenCode },
    };

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task Authenticate_InvalidToken_Returns401InStandardShape(string scenario, string? token, string errorCode)
    {
        await using var app = await StartAsync();

        var response = await GetAsync(app, "/test/me", token);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, scenario);
        response.Headers.WwwAuthenticate.ShouldNotBeEmpty();
        var problem = await TestApp.ReadProblemAsync(response);
        TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
        problem.GetProperty("errorCode").GetString().ShouldBe(errorCode, scenario);
    }

    [Fact]
    public async Task Authorize_EndpointWithoutAttribute_RequiresAuthenticatedUserByFallbackPolicy()
    {
        await using var app = await StartAsync();

        var response = await GetAsync(app, "/test/value", token: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authorize_MissingRole_Returns403InStandardShape()
    {
        await using var app = await StartAsync();

        var response = await GetAsync(app, "/test/admin", CreateToken());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var problem = await TestApp.ReadProblemAsync(response);
        TestApp.PropertyNames(problem).ShouldBe(TestApp.ErrorBodyProperties, ignoreOrder: true);
        problem.GetProperty("errorCode").GetString().ShouldBe(JwtAuthenticationExtensions.ForbiddenCode);
    }

    [Fact]
    public async Task HealthEndpoints_WithoutToken_AreAnonymous()
    {
        await using var app = await StartAsync();

        (await GetAsync(app, ServiceDefaultsExtensions.LivenessPath, token: null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetAsync(app, ServiceDefaultsExtensions.ReadinessPath, token: null)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Start_SigningKeyTooShort_FailsAtStartup()
    {
        var exception = await Should.ThrowAsync<OptionsValidationException>(() => StartAsync(signingKey: "too-short"));

        exception.Message.ShouldContain("SigningKey");
    }
}
