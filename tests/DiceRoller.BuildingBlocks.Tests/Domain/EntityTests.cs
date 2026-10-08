using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.BuildingBlocks.Tests.Domain;

public sealed class EntityTests
{
    [Fact]
    public void Equals_SameTypeAndId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var left = new Order(id);
        var right = new Order(id);

        left.Equals(right).ShouldBeTrue();
        (left == right).ShouldBeTrue();
        (left != right).ShouldBeFalse();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var left = new Order(Guid.NewGuid());
        var right = new Order(Guid.NewGuid());

        (left == right).ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameIdDifferentType_ReturnsFalse()
    {
        var id = Guid.NewGuid();

        new Order(id).Equals(new Invoice(id)).ShouldBeFalse();
    }

    [Fact]
    public void Equals_TwoTransientEntities_ReturnsFalse()
    {
        var left = new Order(Guid.Empty);
        var right = new Order(Guid.Empty);

        (left == right).ShouldBeFalse();
        left.Equals(left).ShouldBeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var entity = new Order(Guid.NewGuid());

        entity.Equals(null).ShouldBeFalse();
        (entity == null).ShouldBeFalse();
    }

    private sealed class Order(Guid id) : Entity<Guid>(id);

    private sealed class Invoice(Guid id) : Entity<Guid>(id);
}
