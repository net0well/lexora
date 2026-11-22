using Lexora.Application.Abstractions.Identity;
using Lexora.Infrastructure.Identity.Hashing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.IntegrationTests.Identity;

public sealed class PasswordHasherAdapterTests
{
    private const string Password = "Str0ngPassword!";

    private readonly PasswordHasherAdapter _hasher = new();

    [Fact]
    public void Verify_HashOfTheSamePassword_Succeeds() =>
        _hasher.Verify(_hasher.Hash(Password), Password).ShouldBe(PasswordVerification.Succeeded);

    [Fact]
    public void Verify_WrongPassword_Fails() =>
        _hasher.Verify(_hasher.Hash(Password), "Str0ngPassword?").ShouldBe(PasswordVerification.Failed);

    [Fact]
    public void Hash_SamePasswordTwice_UsesDifferentSalts() => _hasher.Hash(Password).ShouldNotBe(_hasher.Hash(Password));

    [Fact]
    public void Hash_Password_NeverContainsThePassword() => _hasher.Hash(Password).ShouldNotContain(Password);

    [Theory]
    [InlineData("not-a-hash")]
    [InlineData("")]
    public void Verify_MalformedHash_Fails(string passwordHash) =>
        _hasher.Verify(passwordHash, Password).ShouldBe(PasswordVerification.Failed);

    [Fact]
    public void Verify_HashInOlderFormat_RequestsRehash()
    {
        var legacyHasher = new PasswordHasher<object>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2,
        }));
        var legacyHash = legacyHasher.HashPassword(new object(), Password);

        _hasher.Verify(legacyHash, Password).ShouldBe(PasswordVerification.SucceededRehashNeeded);
    }

    [Fact]
    public void Hash_BlankPassword_Throws() => Should.Throw<ArgumentException>(() => _hasher.Hash(" "));
}
