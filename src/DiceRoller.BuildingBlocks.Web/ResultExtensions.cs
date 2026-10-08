using DiceRoller.BuildingBlocks.Domain;
using Microsoft.AspNetCore.Mvc;

namespace DiceRoller.BuildingBlocks.Web;

/// <summary>
/// Turns a <see cref="Result"/> into an MVC action result. A failure is always written through <see cref="ErrorMapper"/>.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Returns 204 No Content on success, otherwise the mapped error.</summary>
    /// <param name="result">The result.</param>
    /// <returns>The action result.</returns>
    public static IActionResult ToActionResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? new NoContentResult() : ErrorMapper.ToActionResult(result.Error);
    }

    /// <summary>Returns 200 OK with the value on success, otherwise the mapped error.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>The action result.</returns>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? new OkObjectResult(result.Value) : ErrorMapper.ToActionResult(result.Error);
    }

    /// <summary>Returns 201 Created with a <c>Location</c> header and no body on success, otherwise the mapped error.</summary>
    /// <param name="result">The result.</param>
    /// <param name="routeName">The name of the route that locates the created resource.</param>
    /// <param name="routeValues">The route values of the created resource.</param>
    /// <returns>The action result.</returns>
    public static IActionResult ToCreatedResult(this Result result, string routeName, object? routeValues)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess
            ? new CreatedAtRouteResult(routeName, routeValues, value: null)
            : ErrorMapper.ToActionResult(result.Error);
    }

    /// <summary>Returns 201 Created with a <c>Location</c> header and the value on success, otherwise the mapped error.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="routeName">The name of the route that locates the created resource.</param>
    /// <param name="routeValues">The route values of the created resource.</param>
    /// <returns>The action result.</returns>
    public static IActionResult ToCreatedResult<T>(this Result<T> result, string routeName, object? routeValues)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess
            ? new CreatedAtRouteResult(routeName, routeValues, result.Value)
            : ErrorMapper.ToActionResult(result.Error);
    }
}
