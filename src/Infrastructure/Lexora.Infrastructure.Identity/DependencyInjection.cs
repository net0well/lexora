using Lexora.Application.Abstractions.Identity;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Identity.Hashing;
using Lexora.Infrastructure.Identity.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lexora.Infrastructure.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                jwt => JwtSigningKeyProvider.IsValid(jwt.SigningKey),
                $"'{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}' must be a base64 value of at least {JwtSigningKeyProvider.MinimumKeyBytes} bytes.")
            .ValidateOnStart();

        services.AddSingleton<JwtSigningKeyProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddSingleton<ISecretTokenGenerator, SecretTokenGenerator>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        AddAuthentication(services);
        AddAuthorization(services);

        return services;
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(AuthenticationSchemes.Bearer).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(AuthenticationSchemes.Bearer)
            .Configure<IOptions<JwtOptions>, JwtSigningKeyProvider>((bearer, jwtOptions, signingKeys) =>
            {
                var jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = signingKeys.Key,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = jwt.ClockSkew,
                    NameClaimType = ClaimNames.Name,
                    RoleClaimType = ClaimNames.Role,
                };
            });
    }

    private static void AddAuthorization(IServiceCollection services) =>
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.RequireAdmin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(UserRole.Admin)))
            .AddPolicy(AuthorizationPolicies.RequireMember, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(UserRole.Admin), nameof(UserRole.Member)))
            .AddPolicy(AuthorizationPolicies.RequireViewer, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(UserRole.Admin), nameof(UserRole.Member), nameof(UserRole.Viewer)));
}
