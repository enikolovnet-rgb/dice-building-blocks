using System.Diagnostics;
using DiceRoller.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace DiceRoller.BuildingBlocks.Web;

/// <summary>
/// The only place where an <see cref="Error"/> becomes an HTTP response. Every failure (a failed <see cref="Result"/>,
/// a <see cref="DomainException"/>, a validation failure, an authentication failure or an unexpected exception)
/// goes through here, so all error bodies have the same RFC 9457 shape:
/// <c>status</c>, <c>title</c>, <c>detail</c>, <c>errorCode</c>, <c>errors</c> and <c>traceId</c>.
/// </summary>
public static class ErrorMapper
{
    /// <summary>The media type of every error body.</summary>
    public const string ContentType = "application/problem+json";

    /// <summary>The ProblemDetails extension that carries <see cref="Error.Code"/>.</summary>
    public const string ErrorCodeKey = "errorCode";

    /// <summary>The ProblemDetails extension that carries <see cref="Error.Details"/> (property → messages).</summary>
    public const string ErrorsKey = "errors";

    /// <summary>The ProblemDetails extension that carries the trace identifier of the request.</summary>
    public const string TraceIdKey = "traceId";

    /// <summary>Returns the HTTP status code for an <see cref="ErrorType"/>.</summary>
    /// <param name="type">The error type.</param>
    /// <returns>400, 401, 403, 404, 409 or 500.</returns>
    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Builds the RFC 9457 body for <paramref name="error"/>.</summary>
    /// <param name="error">The error.</param>
    /// <param name="httpContext">The current request; supplies the trace identifier.</param>
    /// <returns>The problem details.</returns>
    public static ProblemDetails ToProblemDetails(Error error, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        var status = ToStatusCode(error.Type);
        return new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = error.Message,
            Extensions =
            {
                [ErrorCodeKey] = error.Code,
                [ErrorsKey] = error.Details,
                [TraceIdKey] = Activity.Current?.Id ?? httpContext.TraceIdentifier,
            },
        };
    }

    /// <summary>Returns an MVC action result that writes <paramref name="error"/> as the response.</summary>
    /// <param name="error">The error.</param>
    /// <returns>The action result.</returns>
    public static IActionResult ToActionResult(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ErrorActionResult(error);
    }

    /// <summary>
    /// Writes <paramref name="error"/> as the response. Use it outside MVC: middleware, exception handlers,
    /// authentication events.
    /// </summary>
    /// <param name="httpContext">The current request.</param>
    /// <param name="error">The error.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the body is written.</returns>
    public static Task WriteAsync(HttpContext httpContext, Error error, CancellationToken cancellationToken = default) =>
        WriteAsync(httpContext, error, configure: null, cancellationToken);

    internal static Task WriteAsync(
        HttpContext httpContext,
        Error error,
        Action<ProblemDetails>? configure,
        CancellationToken cancellationToken)
    {
        var problem = ToProblemDetails(error, httpContext);
        configure?.Invoke(problem);

        httpContext.Response.StatusCode = problem.Status!.Value;
        return httpContext.Response.WriteAsJsonAsync(problem, options: null, ContentType, cancellationToken);
    }
}
