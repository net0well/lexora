using Lexora.Infrastructure.Identity.Tokens;

namespace Lexora.Infrastructure.IntegrationTests.Identity;

public sealed class SecretTokenGeneratorTests
{
    private readonly SecretTokenGenerator _generator = new();

    [Fact]
    public void Generate_Token_IsUrlSafeAndCarries64RandomBytes()
    {
        var token = _generator.Generate();

        token.Value.Length.ShouldBe(86);
        token.Value.ShouldAllBe(character => char.IsAsciiLetterOrDigit(character) || character == '-' || character == '_');
    }

    [Fact]
    public void Generate_TwoTokens_AreDifferent() => _generator.Generate().Value.ShouldNotBe(_generator.Generate().Value);

    [Fact]
    public void Generate_Token_ComesWithTheHashOfItsValue()
    {
        var token = _generator.Generate();

        token.Hash.ShouldBe(_generator.Hash(token.Value));
        token.Hash.Length.ShouldBe(64);
        token.Hash.ShouldAllBe(character => char.IsAsciiHexDigitLower(character) || char.IsAsciiDigit(character));
        token.Hash.ShouldNotContain(token.Value);
    }

    [Fact]
    public void Hash_KnownValue_IsItsSha256InLowercaseHex() =>
        _generator.Hash("hello").ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");

    [Fact]
    public void Hash_BlankValue_Throws() => Should.Throw<ArgumentException>(() => _generator.Hash(" "));
}
