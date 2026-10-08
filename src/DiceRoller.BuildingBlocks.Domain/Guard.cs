using System.Diagnostics.CodeAnalysis;

namespace DiceRoller.BuildingBlocks.Domain;

/// <summary>
/// Protects domain invariants.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws a <see cref="DomainException"/> carrying <paramref name="error"/> when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">The condition that breaks the invariant.</param>
    /// <param name="error">The error describing the broken invariant.</param>
    /// <exception cref="DomainException"><paramref name="condition"/> is <see langword="true"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static void Against([DoesNotReturnIf(true)] bool condition, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (condition)
        {
            throw new DomainException(error);
        }
    }
}
