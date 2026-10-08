using DiceRoller.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiceRoller.BuildingBlocks.Web;

/// <summary>
/// Turns exceptions into error responses through <see cref="ErrorMapper"/>:
/// a <see cref="DomainException"/> becomes the same response as a failed <see cref="Result"/> carrying its error,
/// a <see cref="BadHttpRequestException"/> becomes a validation error, and anything else becomes
/// <see cref="Error.Unexpected"/>. Exception details are included only in the Development environment.
/// </summary>
/// <param name="logger">Logs handled exceptions.</param>
/// <param name="environment">Decides whether exception details are exposed.</param>
public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    : IExceptionHandler
{
    /// <summary>Error code used when the request could not be read (malformed body, bad parameter, too large).</summary>
    public const string BadRequestCode = "Request.Malformed";

    /// <summary>The ProblemDetails extension that carries exception details in the Development environment.</summary>
    public const string ExceptionKey = "exception";

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        Action<ProblemDetails>? addDetails = null;
        Error error;

        switch (exception)
        {
            case DomainException domainException:
                LogDomainException(logger, domainException.Error.Code, domainException);
                error = domainException.Error;
                break;

            case BadHttpRequestException badRequest:
                LogBadRequest(logger, badRequest);
                error = Error.Validation(BadRequestCode, "The request could not be read.");
                break;

            default:
                LogUnexpected(logger, exception);
                error = Error.Unexpected();
                if (environment.IsDevelopment())
                {
                    addDetails = problem => problem.Extensions[ExceptionKey] = new
                    {
                        type = exception.GetType().FullName,
                        message = exception.Message,
                        stackTrace = exception.ToString(),
                    };
                }

                break;
        }

        await ErrorMapper.WriteAsync(httpContext, error, addDetails, cancellationToken);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Domain invariant broken: {ErrorCode}")]
    private static partial void LogDomainException(ILogger logger, string errorCode, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bad request")]
    private static partial void LogBadRequest(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnexpected(ILogger logger, Exception exception);
}
