using Lexora.Domain.Users;

namespace Lexora.Domain.UnitTests.Users;

public sealed class PersonNameTests
{
    [Fact]
    public void Create_NameWithSurroundingSpaces_Trims()
    {
        var result = PersonName.Create("  Ana Souza  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("Ana Souza");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Create_Blank_FailsAsRequired(string? value) =>
        PersonName.Create(value).Notifications.Single().Key.ShouldBe("user.name.required");

    [Fact]
    public void Create_BelowMinimumLength_FailsAsTooShort() =>
        PersonName.Create("A").Notifications.Single().Key.ShouldBe("user.name.tooShort");

    [Fact]
    public void Create_OverMaximumLength_FailsAsTooLong() =>
        PersonName.Create(new string('a', PersonName.MaxLength + 1)).Notifications.Single().Key.ShouldBe("user.name.tooLong");

    [Theory]
    [InlineData(PersonName.MinLength)]
    [InlineData(PersonName.MaxLength)]
    public void Create_AtLengthBoundary_Succeeds(int length) =>
        PersonName.Create(new string('a', length)).IsSuccess.ShouldBeTrue();
}
