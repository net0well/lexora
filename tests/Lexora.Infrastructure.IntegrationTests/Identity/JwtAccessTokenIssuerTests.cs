using System.Security.Cryptography;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Identity;
using Lexora.Infrastructure.Identity.Tokens;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lexora.Infrastructure.IntegrationTests.Identity;

public sealed class JwtAccessTokenIssuerTests
{
    private static readonly JwtOptions _options = new()
    {
        Issuer = "lexora",
        Audience = "lexora",
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
    };

    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly JwtSigningKeyProvider _keys = new(Options.Create(_options));

    [Fact]
    public async Task Issue_User_CreatesSignedTokenWithIdentityClaims()
    {
        var user = new UserBuilder().WithRole(UserRole.Admin).Build();

        var token = new JwtAccessTokenIssuer(Options.Create(_options), _keys, _time).Issue(user);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "lexora",
            ValidAudience = "lexora",
            IssuerSigningKey = _keys.Key,
            ValidateLifetime = false,
        });

        validation.IsValid.ShouldBeTrue();
        validation.Claims[ClaimNames.Subject].ShouldBe(user.Id.ToString());
        validation.Claims[ClaimNames.Email].ShouldBe(user.Email.Value);
        validation.Claims[ClaimNames.Name].ShouldBe(user.Name.Value);
        validation.Claims[ClaimNames.Role].ShouldBe("Admin");
        validation.Claims.ShouldContainKey(ClaimNames.TokenId);
    }

    [Fact]
    public void Issue_User_ExpiresAfterTheConfiguredLifetime()
    {
        var token = new JwtAccessTokenIssuer(Options.Create(_options), _keys, _time).Issue(new UserBuilder().Build());

        var jwt = new JsonWebToken(token.Value);

        token.ExpiresAt.ShouldBe(TestClock.Now.AddMinutes(15));
        jwt.ValidTo.ShouldBe(TestClock.Now.AddMinutes(15).UtcDateTime);
        jwt.IssuedAt.ShouldBe(TestClock.Now.UtcDateTime);
    }

    [Fact]
    public async Task Issue_TokenValidatedWithAnotherKey_IsRejected()
    {
        var token = new JwtAccessTokenIssuer(Options.Create(_options), _keys, _time).Issue(new UserBuilder().Build());

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = "lexora",
            ValidAudience = "lexora",
            IssuerSigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)),
            ValidateLifetime = false,
        });

        validation.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("c2hvcnQta2V5")]
    public void SigningKey_MissingMalformedOrShorterThan32Bytes_IsRejected(string signingKey)
    {
        JwtSigningKeyProvider.IsValid(signingKey).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => new JwtSigningKeyProvider(Options.Create(new JwtOptions { SigningKey = signingKey })));
    }

    [Fact]
    public void SigningKey_Base64OfAtLeast32Bytes_IsAccepted() =>
        JwtSigningKeyProvider.IsValid(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))).ShouldBeTrue();
}
