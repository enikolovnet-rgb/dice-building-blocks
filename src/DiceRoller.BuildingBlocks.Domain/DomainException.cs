namespace DiceRoller.BuildingBlocks.Domain;

/// <summary>
/// Thrown when a domain invariant is broken. Carries the <see cref="Domain.Error"/> that describes the violation;
/// the web layer turns it into the same response as a failed <see cref="Result"/>.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>Creates the exception. Its message is <see cref="Error.Message"/>.</summary>
    /// <param name="error">The error describing the broken invariant.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public DomainException(Error error)
        : this(error, innerException: null)
    {
    }

    /// <summary>Creates the exception with an inner exception. Its message is <see cref="Error.Message"/>.</summary>
    /// <param name="error">The error describing the broken invariant.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public DomainException(Error error, Exception? innerException)
        : base(error?.Message, innerException)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    /// <summary>The error describing the broken invariant.</summary>
    public Error Error { get; }
}
