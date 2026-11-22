using Lexora.Application.Abstractions.Identity;
using Lexora.Infrastructure.Identity.Hashing;
using Lexora.Infrastructure.Identity.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
