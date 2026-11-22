using Lexora.Domain.Users;

namespace Lexora.Domain.UnitTests.Users;

public sealed class EmailTests
{
    [Fact]
    public void Create_MixedCaseWithSpaces_NormalizesToTrimmedLowercase()
    {
        var result = Email.Create("  Ana.Souza@Lexora.Local ");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("ana.souza@lexora.local");
        result.Value.ToString().ShouldBe("ana.souza@lexora.local");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ana.lexora.local")]
    [InlineData("@lexora.local")]
    [InlineData("ana@")]
    [InlineData("ana souza@lexora.local")]
    [InlineData("Ana <ana@lexora.local>")]
    [InlineData("ana@lexora@local")]
    public void Create_MalformedAddress_FailsWithInvalidKey(string? value)
    {
        var result = Email.Create(value);

        result.IsFailure.ShouldBeTrue();
        result.Notifications.Single().Key.ShouldBe("user.email.invalid");
    }

    [Fact]
    public void Create_AtMaximumLength_Succeeds()
    {
        var address = $"a@{new string('b', Email.MaxLength - 6)}.com";

        Email.Create(address).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_OverMaximumLength_Fails()
    {
        var address = $"a@{new string('b', Email.MaxLength - 5)}.com";

        Email.Create(address).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Equals_SameAddressDifferentCase_ReturnsTrue() =>
        Email.Create("ANA@LEXORA.LOCAL").Value.ShouldBe(Email.Create("ana@lexora.local").Value);
}
