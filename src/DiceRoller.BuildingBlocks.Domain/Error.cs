namespace DiceRoller.BuildingBlocks.Domain;

/// <summary>
/// Describes why an operation failed. Errors are values: two errors with the same code, message,
/// type and details are equal.
/// </summary>
public sealed record Error
{
    private static readonly IReadOnlyDictionary<string, string[]> EmptyDetails =
        new Dictionary<string, string[]>().AsReadOnly();

    /// <summary>
    /// Creates an error.
    /// </summary>
    /// <param name="code">A stable, machine-readable code, for example <c>"Roll.InvalidSides"</c>.</param>
    /// <param name="message">A human-readable description of the failure.</param>
    /// <param name="type">The category of the failure.</param>
    /// <param name="details">Optional per-field messages, keyed by property name. <see langword="null"/> means none.</param>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is null or whitespace.</exception>
    public Error(string code, string message, ErrorType type, IReadOnlyDictionary<string, string[]>? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Type = type;
        Details = details ?? EmptyDetails;
    }

    /// <summary>A stable, machine-readable code identifying the failure.</summary>
    public string Code { get; }

    /// <summary>A human-readable description of the failure.</summary>
    public string Message { get; }

    /// <summary>The category of the failure.</summary>
    public ErrorType Type { get; }

    /// <summary>Per-field messages, keyed by property name. Empty when there are none; never <see langword="null"/>.</summary>
    public IReadOnlyDictionary<string, string[]> Details { get; }

    /// <summary>Creates an <see cref="ErrorType.Validation"/> error.</summary>
    /// <param name="code">A stable, machine-readable code.</param>
    /// <param name="message">A human-readable description.</param>
    /// <param name="details">Optional per-field messages, keyed by property name.</param>
    /// <returns>The error.</returns>
    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]>? details = null) =>
        new(code, message, ErrorType.Validation, details);

    /// <summary>Creates an <see cref="ErrorType.NotFound"/> error.</summary>
    /// <param name="code">A stable, machine-readable code.</param>
    /// <param name="message">A human-readable description.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates an <see cref="ErrorType.Conflict"/> error.</summary>
    /// <param name="code">A stable, machine-readable code.</param>
    /// <param name="message">A human-readable description.</param>
    /// <returns>The error.</returns>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Creates an <see cref="ErrorType.Unauthorized"/> error.</summary>
    /// <param name="code">A stable, machine-readable code.</param>
    /// <param name="message">A human-readable description.</param>
    /// <returns>The error.</returns>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates an <see cref="ErrorType.Forbidden"/> error.</summary>
    /// <param name="code">A stable, machine-readable code.</param>
    /// <param name="message">A human-readable description.</param>
    /// <returns>The error.</returns>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>Creates an <see cref="ErrorType.Unexpected"/> error.</summary>
    /// <param name="code">A stable, machine-readable code. Defaults to <c>"General.Unexpected"</c>.</param>
    /// <param name="message">A human-readable description. Defaults to a generic message.</param>
    /// <returns>The error.</returns>
    public static Error Unexpected(string code = "General.Unexpected", string message = "An unexpected error occurred.") =>
        new(code, message, ErrorType.Unexpected);

    /// <summary>
    /// Determines whether two errors are equal. <see cref="Details"/> is compared by content:
    /// same keys, and for each key the same messages in the same order.
    /// </summary>
    /// <param name="other">The error to compare with.</param>
    /// <returns><see langword="true"/> if the errors are equal.</returns>
    public bool Equals(Error? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
            && Code == other.Code
            && Message == other.Message
            && Type == other.Type
            && DetailsEqual(Details, other.Details);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Code);
        hash.Add(Message);
        hash.Add(Type);
        hash.Add(Details.Count);
        return hash.ToHashCode();
    }

    private static bool DetailsEqual(IReadOnlyDictionary<string, string[]> left, IReadOnlyDictionary<string, string[]> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var (key, messages) in left)
        {
            if (!right.TryGetValue(key, out var otherMessages) || !messages.AsSpan().SequenceEqual(otherMessages))
            {
                return false;
            }
        }

        return true;
    }
}
