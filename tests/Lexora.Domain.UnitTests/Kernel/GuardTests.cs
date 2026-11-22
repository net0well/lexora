using Lexora.Shared.Guards;

namespace Lexora.Domain.UnitTests.Kernel;

public sealed class GuardTests
{
    [Fact]
    public void NotNull_WithValue_ReturnsValue()
    {
        var value = new object();

        Guard.NotNull(value).ShouldBeSameAs(value);
    }

    [Fact]
    public void NotNull_WithNull_ThrowsWithArgumentName()
    {
        string? missing = null;

        var exception = Should.Throw<ArgumentNullException>(() => Guard.NotNull(missing));

        exception.ParamName.ShouldBe(nameof(missing));
    }

    [Fact]
    public void NotEmpty_WithIdentifier_ReturnsIdentifier()
    {
        var id = Guid.CreateVersion7();

        Guard.NotEmpty(id).ShouldBe(id);
    }

    [Fact]
    public void NotEmpty_WithEmptyIdentifier_ThrowsWithArgumentName()
    {
        var userId = Guid.Empty;

        var exception = Should.Throw<ArgumentException>(() => Guard.NotEmpty(userId));

        exception.ParamName.ShouldBe(nameof(userId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_WithBlankValue_Throws(string? value) =>
        Should.Throw<ArgumentException>(() => Guard.NotNullOrWhiteSpace(value));

    [Fact]
    public void NotNullOrWhiteSpace_WithText_ReturnsText() => Guard.NotNullOrWhiteSpace("hash").ShouldBe("hash");

    [Fact]
    public void MaxLength_AtLimit_ReturnsValue() => Guard.MaxLength("abc", 3).ShouldBe("abc");

    [Fact]
    public void MaxLength_OverLimit_Throws() => Should.Throw<ArgumentOutOfRangeException>(() => Guard.MaxLength("abcd", 3));

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void NotNegative_WithZeroOrMore_ReturnsValue(int value) => Guard.NotNegative(value).ShouldBe(value);

    [Fact]
    public void NotNegative_WithNegative_Throws() => Should.Throw<ArgumentOutOfRangeException>(() => Guard.NotNegative(-1L));

    [Fact]
    public void Positive_WithPositiveNumber_ReturnsValue() => Guard.Positive(1).ShouldBe(1);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Positive_WithZeroOrNegative_Throws(int value) => Should.Throw<ArgumentOutOfRangeException>(() => Guard.Positive(value));

    [Fact]
    public void Positive_WithPositiveDuration_ReturnsDuration() =>
        Guard.Positive(TimeSpan.FromMinutes(15)).ShouldBe(TimeSpan.FromMinutes(15));

    [Fact]
    public void Positive_WithZeroDuration_Throws() => Should.Throw<ArgumentOutOfRangeException>(() => Guard.Positive(TimeSpan.Zero));

    [Fact]
    public void Defined_WithDefinedValue_ReturnsValue() => Guard.Defined(SampleColor.Green).ShouldBe(SampleColor.Green);

    [Fact]
    public void Defined_WithUndefinedValue_Throws() => Should.Throw<ArgumentOutOfRangeException>(() => Guard.Defined((SampleColor)99));
}
