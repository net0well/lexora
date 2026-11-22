using Lexora.Application.Auth.Services;
using Lexora.Application.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                auth => auth.RefreshTokenLifetime > TimeSpan.Zero && auth.Lockout.Duration > TimeSpan.Zero,
                $"'{AuthOptions.SectionName}' durations must be positive.")
            .ValidateOnStart();

        AddServices(services);

        return services;
    }

    private static void AddServices(IServiceCollection services) =>
        services.AddScoped<IAuthService, AuthService>();
}
