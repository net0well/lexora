namespace Lexora.Domain.UnitTests.Kernel;

public sealed class EntityTests
{
    private static readonly Guid _id = Guid.CreateVersion7();

    [Fact]
    public void Equals_SameTypeAndId_ReturnsTrue()
    {
        var left = new SampleEntity(_id);
        var right = new SampleEntity(_id);

        left.Equals(right).ShouldBeTrue();
        (left == right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentIds_ReturnsFalse()
    {
        var left = new SampleEntity(_id);
        var right = new SampleEntity(Guid.CreateVersion7());

        left.Equals(right).ShouldBeFalse();
        (left != right).ShouldBeTrue();
    }

    [Fact]
    public void Equals_SameIdDifferentType_ReturnsFalse()
    {
        var left = new SampleEntity(_id);
        var right = new OtherSampleEntity(_id);

        left.Equals(right).ShouldBeFalse();
    }

    [Fact]
    public void Equals_TransientEntities_ComparesByReference()
    {
        var entity = new SampleEntity();
        var other = new SampleEntity();

        entity.Equals(entity).ShouldBeTrue();
        entity.Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_NullOperands_HandlesNulls()
    {
        SampleEntity? missing = null;

        (missing == null).ShouldBeTrue();
        (new SampleEntity(_id) == missing).ShouldBeFalse();
    }
}
