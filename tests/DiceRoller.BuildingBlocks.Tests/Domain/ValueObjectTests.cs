using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.BuildingBlocks.Tests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void Equals_SameComponents_ReturnsTrue()
    {
        var left = new Money(10m, "EUR");
        var right = new Money(10m, "eur");

        left.Equals(right).ShouldBeTrue();
        (left == right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentComponents_ReturnsFalse()
    {
        var left = new Money(10m, "EUR");
        var right = new Money(11m, "EUR");

        (left != right).ShouldBeTrue();
    }

    [Fact]
    public void Equals_SameComponentsDifferentType_ReturnsFalse()
    {
        new Money(10m, "EUR").Equals(new Price(10m, "EUR")).ShouldBeFalse();
    }

    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return amount;
            yield return currency.ToUpperInvariant();
        }
    }

    private sealed class Price(decimal amount, string currency) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return amount;
            yield return currency.ToUpperInvariant();
        }
    }
}
