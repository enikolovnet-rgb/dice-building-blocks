namespace DiceRoller.BuildingBlocks.Domain;

/// <summary>
/// Base class for entities: objects defined by their identity rather than their attributes.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
/// <remarks>
/// Two entities are equal when they have the same runtime type and the same, non-default <see cref="Id"/>.
/// A transient entity (whose <see cref="Id"/> is still the default value) is equal only to itself.
/// </remarks>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>Creates an entity with the given identifier.</summary>
    /// <param name="id">The identifier.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>Creates an entity without an identifier. Intended for ORM materialisation.</summary>
    protected Entity()
    {
        Id = default!;
    }

    /// <summary>The identifier of the entity.</summary>
    public TId Id { get; protected init; }

    /// <summary>Determines whether two entities are equal.</summary>
    /// <param name="left">The first entity.</param>
    /// <param name="right">The second entity.</param>
    /// <returns><see langword="true"/> if the entities are equal.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    /// <summary>Determines whether two entities are not equal.</summary>
    /// <param name="left">The first entity.</param>
    /// <param name="right">The second entity.</param>
    /// <returns><see langword="true"/> if the entities are not equal.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null || other.GetType() != GetType() || IsTransient() || other.IsTransient())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        IsTransient() ? base.GetHashCode() : HashCode.Combine(GetType(), Id);

    private bool IsTransient() => EqualityComparer<TId>.Default.Equals(Id, default);
}
