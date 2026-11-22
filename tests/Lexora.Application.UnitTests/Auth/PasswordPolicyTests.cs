using Lexora.Application.Auth;

namespace Lexora.Application.UnitTests.Auth;

public sealed class PasswordPolicyTests
{
    private const string Email = "ana.souza@lexora.local";

    [Theory]
    [InlineData("Str0ngPassword")]
    [InlineData("correct Horse 9 battery")]
    public void IsStrong_PasswordMeetingEveryRule_ReturnsTrue(string password) =>
        PasswordPolicy.IsStrong(password, Email).ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sh0rtPasswd")]
    [InlineData("str0ngpassword")]
    [InlineData("STR0NGPASSWORD")]
    [InlineData("StrongPassword")]
    [InlineData("X1ana.souza@lexora.local")]
    [InlineData("X1ANA.SOUZA@LEXORA.LOCAL")]
    public void IsStrong_PasswordBreakingARule_ReturnsFalse(string? password) =>
        PasswordPolicy.IsStrong(password, Email).ShouldBeFalse();

    [Fact]
    public void IsStrong_WithoutEmail_SkipsTheEmailRule() =>
        PasswordPolicy.IsStrong("X1ana.souza@lexora.local", null).ShouldBeTrue();

    [Fact]
    public void StrongPasswordRule_WeakPassword_ReportsTheWeakPasswordKey()
    {
        var result = new SampleValidator().Validate(new SampleInput(Email, "weak"));

        result.Errors.Single().ErrorCode.ShouldBe("auth.password.weak");
    }

    [Fact]
    public void StrongPasswordRule_PasswordContainingTheModelEmail_IsRejected() =>
        new SampleValidator().Validate(new SampleInput(Email, $"A1{Email}")).IsValid.ShouldBeFalse();

    [Fact]
    public void StrongPasswordRule_StrongPassword_Passes() =>
        new SampleValidator().Validate(new SampleInput(Email, "Str0ngPassword")).IsValid.ShouldBeTrue();
}
