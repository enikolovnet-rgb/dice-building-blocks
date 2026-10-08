using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

namespace DiceRoller.BuildingBlocks.Web;

/// <summary>
/// The hosting setup every DiceRoller service shares: error handling, controllers with validation, OpenAPI,
/// health checks, OpenTelemetry, forwarded headers and correlation ids.
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>The header that carries the correlation id on requests and responses.</summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>The liveness endpoint. It runs no health checks.</summary>
    public const string LivenessPath = "/health/live";

    /// <summary>The readiness endpoint. It runs the health checks tagged with <see cref="ReadyTag"/>.</summary>
    public const string ReadinessPath = "/health/ready";

    /// <summary>Tag a service puts on its own health checks (for example its database) so readiness runs them.</summary>
    public const string ReadyTag = "ready";

    /// <summary>The configuration key that enables the OTLP exporter when it has a value.</summary>
    public const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>
    /// Registers the shared services: ProblemDetails and <see cref="GlobalExceptionHandler"/>; controllers with
    /// <see cref="ValidationFilter"/> as a global filter and ASP.NET Core's automatic 400 response turned off;
    /// OpenAPI with a Bearer security scheme; health checks; OpenTelemetry traces, metrics and logs (exported over
    /// OTLP only when <see cref="OtlpEndpointKey"/> is configured); <see cref="TimeProvider.System"/>; forwarded headers.
    /// </summary>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <returns>The host builder.</returns>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        services.AddControllers(options => options.Filters.Add<ValidationFilter>());
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
        services.AddHealthChecks();
        services.AddAuthentication();
        services.AddAuthorization();

        services.TryAddSingleton(TimeProvider.System);
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        AddOpenTelemetry(builder);
        return builder;
    }

    /// <summary>
    /// Adds the shared middleware and endpoints: forwarded headers, correlation id, exception handling,
    /// authentication and authorization, OpenAPI and Scalar (Development only), the health endpoints and controllers.
    /// The health and documentation endpoints allow anonymous access.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseForwardedHeaders();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference().AllowAnonymous();
        }

        app.MapHealthChecks(LivenessPath, new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks(ReadinessPath, new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) })
            .AllowAnonymous();
        app.MapControllers();

        return app;
    }

    private static void AddOpenTelemetry(IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]))
        {
            openTelemetry.UseOtlpExporter();
        }
    }
}
