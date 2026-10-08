namespace DiceRoller.BuildingBlocks.Domain;

/// <summary>
/// The category of an <see cref="Error"/>. The web layer maps each category to one HTTP status code.
/// </summary>
public enum ErrorType
{
    /// <summary>The input is invalid (HTTP 400).</summary>
    Validation,

    /// <summary>The caller is not authenticated (HTTP 401).</summary>
    Unauthorized,

    /// <summary>The caller is authenticated but not allowed to perform the operation (HTTP 403).</summary>
    Forbidden,

    /// <summary>The requested resource does not exist (HTTP 404).</summary>
    NotFound,

    /// <summary>The operation conflicts with the current state of the resource (HTTP 409).</summary>
    Conflict,

    /// <summary>An unexpected failure (HTTP 500).</summary>
    Unexpected,
}
