using System.Text;
using DiceRoller.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DiceRoller.BuildingBlocks.Web;

/// <summary>Registers JWT bearer authentication with the settings every DiceRoller service shares.</summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>Error code of a 401 when the request has no token.</summary>
    public const string MissingTokenCode = "Auth.Unauthorized";

    /// <summary>Error code of a 401 when the token has expired.</summary>
    public const string ExpiredTokenCode = "Auth.TokenExpired";

    /// <summary>Error code of a 401 when the token is invalid (signature, issuer, audience, algorithm).</summary>
    public const string InvalidTokenCode = "Auth.InvalidToken";

    /// <summary>Error code of a 403.</summary>
    public const string ForbiddenCode = "Auth.Forbidden";

    /// <summary>
    /// Binds and validates <see cref="JwtOptions"/> (failing at startup when invalid) and adds JWT bearer authentication:
    /// signature, issuer, audience and lifetime are validated; only HS256 is accepted; clock skew is 30 seconds;
    /// inbound claims are not mapped, so claims keep the names in <see cref="Contracts.JwtClaimNames"/>.
    /// 401 and 403 responses are written through <see cref="ErrorMapper"/>. The fallback authorization policy
    /// requires an authenticated user, so endpoints must opt out with <c>[AllowAnonymous]</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration that contains the <see cref="JwtOptions.SectionName"/> section.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) => Configure(bearer, jwt.Value));

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    private static void Configure(JwtBearerOptions bearer, JwtOptions jwt)
    {
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        bearer.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                var error = context.AuthenticateFailure switch
                {
                    null => Error.Unauthorized(MissingTokenCode, "Authentication is required."),
                    SecurityTokenExpiredException => Error.Unauthorized(ExpiredTokenCode, "The token has expired."),
                    _ => Error.Unauthorized(InvalidTokenCode, "The token is invalid."),
                };

                context.Response.Headers.WWWAuthenticate = context.AuthenticateFailure is null
                    ? "Bearer"
                    : "Bearer error=\"invalid_token\"";
                await ErrorMapper.WriteAsync(context.HttpContext, error, context.HttpContext.RequestAborted);
            },
            OnForbidden = context => ErrorMapper.WriteAsync(
                context.HttpContext,
                Error.Forbidden(ForbiddenCode, "You do not have permission to perform this operation."),
                context.HttpContext.RequestAborted),
        };
    }
}
