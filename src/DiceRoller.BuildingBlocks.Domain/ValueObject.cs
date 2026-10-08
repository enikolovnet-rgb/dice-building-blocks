namespace DiceRoller.BuildingBlocks.Domain;

/// <summary>
/// Base class for value objects: objects defined by their attributes rather than an identity.
/// </summary>
/// <remarks>
/// Prefer a <c>sealed record</c> for new value objects; records already have value equality.
/// Derive from this class only when equality needs custom components, for example a collection compared
/// by content or a case-insensitive string.
/// </remarks>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Determines whether two value objects are equal.</summary>
    /// <param name="left">The first value object.</param>
    /// <param name="right">The second value object.</param>
    /// <returns><see langword="true"/> if the value objects are equal.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    /// <summary>Determines whether two value objects are not equal.</summary>
    /// <param name="left">The first value object.</param>
    /// <param name="right">The second value object.</param>
    /// <returns><see langword="true"/> if the value objects are not equal.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other is not null
            && other.GetType() == GetType()
            && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GetType());
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    /// <summary>Returns the values that define equality, in a stable order.</summary>
    /// <returns>The equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();
}
