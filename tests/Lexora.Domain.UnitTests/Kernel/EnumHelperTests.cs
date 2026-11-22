using Lexora.Shared.Helpers;

namespace Lexora.Domain.UnitTests.Kernel;

public sealed class EnumHelperTests
{
    [Theory]
    [InlineData("Green")]
    [InlineData("green")]
    [InlineData("  GREEN ")]
    public void TryParse_KnownName_ReturnsValueIgnoringCaseAndSpaces(string value)
    {
        var parsed = EnumHelper.TryParse<SampleColor>(value, out var color);

        parsed.ShouldBeTrue();
        color.ShouldBe(SampleColor.Green);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Purple")]
    [InlineData("2")]
    [InlineData("Red,Green")]
    public void TryParse_UnknownOrNumericValue_ReturnsFalse(string? value)
    {
        var parsed = EnumHelper.TryParse<SampleColor>(value, out var color);

        parsed.ShouldBeFalse();
        color.ShouldBe(default);
    }

    [Fact]
    public void Values_Enum_ReturnsAllDefinedValues() =>
        EnumHelper.Values<SampleColor>().ShouldBe([SampleColor.Red, SampleColor.Green, SampleColor.Blue]);

    [Fact]
    public void Description_WithDescriptionAttribute_ReturnsDescription() =>
        EnumHelper.Description(SampleColor.Green).ShouldBe("Dark green");

    [Fact]
    public void Description_WithoutDescriptionAttribute_ReturnsName() =>
        EnumHelper.Description(SampleColor.Red).ShouldBe("Red");
}
