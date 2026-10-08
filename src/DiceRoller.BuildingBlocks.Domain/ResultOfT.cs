namespace DiceRoller.BuildingBlocks.Domain;

/// <summary>
/// The outcome of an operation that returns a value: either a success carrying a <typeparamref name="T"/>
/// or a failure carrying an <see cref="Domain.Error"/>.
/// </summary>
/// <typeparam name="T">The type of the value on success.</typeparam>
/// <remarks>
/// Values and errors convert implicitly, so a handler can <c>return value;</c> or <c>return error;</c>.
/// C# does not allow implicit conversions from interface types; for an interface <typeparamref name="T"/>
/// use <see cref="Result.Success{T}(T)"/>.
/// </remarks>
public sealed class Result<T> : Result
{
    private readonly T _value;

    internal Result(T value)
        : base(isSuccess: true, error: null)
    {
        _value = value;
    }

    internal Result(Error error)
        : base(isSuccess: false, error)
    {
        _value = default!;
    }

    /// <summary>The value of a successful result.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"A failed result has no value. Error: {Error.Code}.");

    /// <summary>Converts a value into a successful result.</summary>
    /// <param name="value">The value.</param>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>Converts an <see cref="Domain.Error"/> into a failed result.</summary>
    /// <param name="error">The reason for the failure.</param>
    public static implicit operator Result<T>(Error error) => new(error);

    /// <summary>Calls <paramref name="onSuccess"/> with the value or <paramref name="onFailure"/> with the error.</summary>
    /// <typeparam name="TOut">The type returned by both branches.</typeparam>
    /// <param name="onSuccess">Called with the value when the result is a success.</param>
    /// <param name="onFailure">Called with the error when the result is a failure.</param>
    /// <returns>The value returned by the branch that was called.</returns>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(_value) : onFailure(Error);
    }
}
