using DiceRoller.BuildingBlocks.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace DiceRoller.BuildingBlocks.Web;

/// <summary>
/// Global action filter that validates the request before the action runs. Model-binding failures (for example a
/// malformed JSON body) and the failures of every registered <see cref="IValidator{T}"/> for the action arguments
/// are folded into one <see cref="ErrorType.Validation"/> error with per-field <see cref="Error.Details"/>,
/// written through <see cref="ErrorMapper"/>.
/// </summary>
/// <remarks>
/// Register validators in the service's DI container as <see cref="IValidator{T}"/>; the filter resolves all of them
/// for the runtime type of each argument.
/// </remarks>
public sealed class ValidationFilter : IAsyncActionFilter
{
    /// <summary>The error code of every validation failure reported by this filter.</summary>
    public const string ErrorCode = "Request.Invalid";

    /// <summary>The message of every validation failure reported by this filter.</summary>
    public const string ErrorMessage = "One or more validation errors occurred.";

    private const string InvalidValueMessage = "The value is invalid.";

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var details = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        AddModelStateErrors(context.ModelState, details);
        await AddValidatorErrorsAsync(context, details);

        if (details.Count > 0)
        {
            var error = Error.Validation(
                ErrorCode,
                ErrorMessage,
                details.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));
            context.Result = ErrorMapper.ToActionResult(error);
            return;
        }

        await next();
    }

    private static void AddModelStateErrors(ModelStateDictionary modelState, Dictionary<string, List<string>> details)
    {
        if (modelState.IsValid)
        {
            return;
        }

        foreach (var (key, entry) in modelState)
        {
            foreach (var modelError in entry.Errors)
            {
                var message = string.IsNullOrWhiteSpace(modelError.ErrorMessage) ? InvalidValueMessage : modelError.ErrorMessage;
                Add(details, key, message);
            }
        }
    }

    private static async Task AddValidatorErrorsAsync(ActionExecutingContext context, Dictionary<string, List<string>> details)
    {
        var services = context.HttpContext.RequestServices;
        var cancellationToken = context.HttpContext.RequestAborted;

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            foreach (var validator in services.GetServices(validatorType).OfType<IValidator>())
            {
                var result = await validator.ValidateAsync(new ValidationContext<object>(argument), cancellationToken);
                foreach (var failure in result.Errors)
                {
                    Add(details, failure.PropertyName, failure.ErrorMessage);
                }
            }
        }
    }

    private static void Add(Dictionary<string, List<string>> details, string key, string message)
    {
        if (!details.TryGetValue(key, out var messages))
        {
            messages = [];
            details[key] = messages;
        }

        messages.Add(message);
    }
}
