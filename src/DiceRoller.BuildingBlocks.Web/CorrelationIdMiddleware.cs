using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DiceRoller.BuildingBlocks.Web;

/// <summary>
/// Reads the <see cref="ServiceDefaultsExtensions.CorrelationIdHeader"/> request header (or creates a new id when it is
/// missing or unusable), echoes it on the response and adds it to the logging scope and the current trace.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const int MaxLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ReadOrCreate(context.Request.Headers[ServiceDefaultsExtensions.CorrelationIdHeader]);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ServiceDefaultsExtensions.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string ReadOrCreate(string? value) =>
        IsUsable(value) ? value! : Guid.NewGuid().ToString("N");

    private static bool IsUsable(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
