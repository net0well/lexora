using System.Security.Claims;
using System.Security.Cryptography;
using Lexora.Application.Abstractions.Identity;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Identity;
using Lexora.Shared;
using Lexora.TestKit.Builders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Lexora.Infrastructure.IntegrationTests.Identity;

public sealed class IdentityAdapterTests : IDisposable
{
    private readonly ServiceProvider _services = BuildServices(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

    [Theory]
    [InlineData(AuthorizationPolicies.RequireAdmin, UserRole.Admin, true)]
    [InlineData(AuthorizationPolicies.RequireAdmin, UserRole.Member, false)]
    [InlineData(AuthorizationPolicies.RequireAdmin, UserRole.Viewer, false)]
    [InlineData(AuthorizationPolicies.RequireMember, UserRole.Admin, true)]
    [InlineData(AuthorizationPolicies.RequireMember, UserRole.Member, true)]
    [InlineData(AuthorizationPolicies.RequireMember, UserRole.Viewer, false)]
    [InlineData(AuthorizationPolicies.RequireViewer, UserRole.Admin, true)]
    [InlineData(AuthorizationPolicies.RequireViewer, UserRole.Member, true)]
    [InlineData(AuthorizationPolicies.RequireViewer, UserRole.Viewer, true)]
    public async Task Policy_UserWithRole_IsAuthorizedAccordingToTheMatrix(string policy, UserRole role, bool expected)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimNames.Subject, Guid.CreateVersion7().ToString()), new Claim(ClaimNames.Role, role.ToString())],
            AuthenticationSchemes.Bearer,
            ClaimNames.Name,
            ClaimNames.Role));

        var result = await _services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, policy);

        result.Succeeded.ShouldBe(expected);
    }

    [Theory]
    [InlineData(AuthorizationPolicies.RequireAdmin)]
    [InlineData(AuthorizationPolicies.RequireMember)]
    [InlineData(AuthorizationPolicies.RequireViewer)]
    public async Task Policy_AnonymousUser_IsNotAuthorized(string policy) =>
        (await _services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), policy))
            .Succeeded.ShouldBeFalse();

    [Fact]
    public async Task BearerValidation_TokenFromTheIssuer_IsAccepted()
    {
        var token = _services.GetRequiredService<IAccessTokenIssuer>().Issue(new UserBuilder().WithRole(UserRole.Member).Build());

        var result = await ValidateAsync(token.Value);

        result.IsValid.ShouldBeTrue();
        result.ClaimsIdentity.FindFirst(ClaimNames.Role)?.Value.ShouldBe("Member");
    }

    [Fact]
    public async Task BearerValidation_TokenSignedWithAnotherKey_IsRejected()
    {
        using var otherServices = BuildServices(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        var foreignToken = otherServices.GetRequiredService<IAccessTokenIssuer>().Issue(new UserBuilder().Build());

        (await ValidateAsync(foreignToken.Value)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task BearerValidation_TokenFromAnotherIssuer_IsRejected()
    {
        var signingKey = _services.GetRequiredService<IOptions<JwtOptions>>().Value.SigningKey;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "someone-else",
            Audience = "lexora",
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(signingKey)), SecurityAlgorithms.HmacSha256),
        });

        (await ValidateAsync(token)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task BearerValidation_ExpiredToken_IsRejected()
    {
        var signingKey = _services.GetRequiredService<IOptions<JwtOptions>>().Value.SigningKey;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "lexora",
            Audience = "lexora",
            IssuedAt = DateTime.UtcNow.AddHours(-2),
            NotBefore = DateTime.UtcNow.AddHours(-2),
            Expires = DateTime.UtcNow.AddHours(-1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(signingKey)), SecurityAlgorithms.HmacSha256),
        });

        (await ValidateAsync(token)).IsValid.ShouldBeFalse();
    }

    public void Dispose() => _services.Dispose();

    private Task<TokenValidationResult> ValidateAsync(string token)
    {
        var bearer = _services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(AuthenticationSchemes.Bearer);

        return new JsonWebTokenHandler().ValidateTokenAsync(token, bearer.TokenValidationParameters);
    }

    private static ServiceProvider BuildServices(string signingKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}"] = signingKey })
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddShared()
            .AddIdentityAdapter(configuration)
            .BuildServiceProvider();
    }
}
