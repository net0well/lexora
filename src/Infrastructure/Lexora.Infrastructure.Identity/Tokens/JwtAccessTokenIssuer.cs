using System.Security.Claims;
using Lexora.Application.Abstractions.Identity;
using Lexora.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lexora.Infrastructure.Identity.Tokens;

public sealed class JwtAccessTokenIssuer(
    IOptions<JwtOptions> options,
    JwtSigningKeyProvider signingKeys,
    TimeProvider timeProvider) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Issue(User user)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now + jwt.AccessTokenLifetime;

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKeys.Key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimNames.Subject, user.Id.ToString()),
                new Claim(ClaimNames.Email, user.Email.Value),
                new Claim(ClaimNames.Name, user.Name.Value),
                new Claim(ClaimNames.Role, user.Role.ToString()),
                new Claim(ClaimNames.TokenId, Guid.CreateVersion7().ToString()),
            ]),
        });

        return new AccessToken(token, expiresAt);
    }
}
