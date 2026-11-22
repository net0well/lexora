using Lexora.Domain.Documents;

namespace Lexora.Domain.UnitTests.Documents;

public sealed class ContentHashTests
{
    private const string Sha256OfHello = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";

    [Fact]
    public void FromHex_LowercaseHash_KeepsIt() => ContentHash.FromHex(Sha256OfHello).Value.ShouldBe(Sha256OfHello);

    [Fact]
    public void FromHex_UppercaseHash_NormalizesToLowercase() =>
        ContentHash.FromHex(Sha256OfHello.ToUpperInvariant()).Value.ShouldBe(Sha256OfHello);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2cf24dba")]
    [InlineData("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b98245")]
    [InlineData("zzf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824")]
    public void FromHex_NotASha256HexString_Throws(string value) =>
        Should.Throw<ArgumentException>(() => ContentHash.FromHex(value));

    [Fact]
    public void Equals_SameHashDifferentCase_ReturnsTrue() =>
        ContentHash.FromHex(Sha256OfHello.ToUpperInvariant()).ShouldBe(ContentHash.FromHex(Sha256OfHello));
}
