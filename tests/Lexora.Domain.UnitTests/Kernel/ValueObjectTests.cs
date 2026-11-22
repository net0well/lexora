namespace Lexora.Domain.UnitTests.Kernel;

public sealed class ValueObjectTests
{
    [Fact]
    public void Equals_SameComponents_ReturnsTrue()
    {
        var left = new SampleRange(1, 10);
        var right = new SampleRange(1, 10);

        left.Equals(right).ShouldBeTrue();
        (left == right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Theory]
    [InlineData(2, 10)]
    [InlineData(1, 11)]
    [InlineData(1, null)]
    public void Equals_DifferentComponent_ReturnsFalse(int start, int? end)
    {
        var left = new SampleRange(1, 10);
        var right = new SampleRange(start, end);

        left.Equals(right).ShouldBeFalse();
        (left != right).ShouldBeTrue();
    }

    [Fact]
    public void Equals_NullComponentsOnBothSides_ReturnsTrue()
    {
        var left = new SampleRange(1, null);
        var right = new SampleRange(1, null);

        left.ShouldBe(right);
    }

    [Fact]
    public void Equals_SameComponentsDifferentType_ReturnsFalse()
    {
        var left = new SampleRange(1, 10);
        var right = new OtherSampleRange(1, 10);

        left.Equals(right).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_NullOperands_HandlesNulls()
    {
        SampleRange? missing = null;

        (missing == null).ShouldBeTrue();
        (new SampleRange(1, 10) == missing).ShouldBeFalse();
    }
}
