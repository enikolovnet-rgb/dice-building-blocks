using DiceRoller.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace DiceRoller.BuildingBlocks.Web;

/// <summary>Writes an <see cref="Error"/> through <see cref="ErrorMapper"/> using MVC's output formatters.</summary>
internal sealed class ErrorActionResult(Error error) : IActionResult
{
    public Error Error { get; } = error;

    public Task ExecuteResultAsync(ActionContext context)
    {
        var problem = ErrorMapper.ToProblemDetails(Error, context.HttpContext);
        var result = new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ErrorMapper.ContentType },
        };

        return result.ExecuteResultAsync(context);
    }
}
