namespace DiceRoller.BuildingBlocks.Domain;

/// <summary>
/// The outcome of an operation that returns no value: either a success or a failure carrying an <see cref="Domain.Error"/>.
/// Use it for expected business-rule failures; use <see cref="DomainException"/> for broken invariants.
/// </summary>
/// <remarks>
/// This is a deliberate base class: <see cref="Result{T}"/> derives from it so code that only cares about
/// success or failure can treat both uniformly. It cannot be derived from outside this assembly.
/// </remarks>
public class Result
{
    private static readonly Result SuccessResult = new(isSuccess: true, error: null);

    private readonly Error? _error;

    private protected Result(bool isSuccess, Error? error)
    {
        if (!isSuccess)
        {
            ArgumentNullException.ThrowIfNull(error);
        }

        IsSuccess = isSuccess;
        _error = error;
    }

    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The reason the operation failed.</summary>
    /// <exception cref="InvalidOperationException">The result is a success.</exception>
    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    /// <summary>Returns a successful result.</summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => SuccessResult;

    /// <summary>Returns a failed result.</summary>
    /// <param name="error">The reason for the failure.</param>
    /// <returns>A failed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result Failure(Error error) => new(isSuccess: false, error);

    /// <summary>Returns a successful result carrying <paramref name="value"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success<T>(T value) => new(value);

    /// <summary>Returns a failed result of <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of the value a success would carry.</typeparam>
    /// <param name="error">The reason for the failure.</param>
    /// <returns>A failed result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<T> Failure<T>(Error error) => new(error);

    /// <summary>Converts an <see cref="Domain.Error"/> into a failed result.</summary>
    /// <param name="error">The reason for the failure.</param>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>Calls <paramref name="onSuccess"/> or <paramref name="onFailure"/> depending on the outcome.</summary>
    /// <typeparam name="TOut">The type returned by both branches.</typeparam>
    /// <param name="onSuccess">Called when the result is a success.</param>
    /// <param name="onFailure">Called with the error when the result is a failure.</param>
    /// <returns>The value returned by the branch that was called.</returns>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess() : onFailure(Error);
    }
}
