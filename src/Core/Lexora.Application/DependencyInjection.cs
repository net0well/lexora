using FluentValidation;
using Lexora.Application.Auth.SeedAdmin;
using Lexora.Application.Auth.Services;
using Lexora.Application.Common.Options;
using Lexora.Application.Users.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                auth => auth.RefreshTokenLifetime > TimeSpan.Zero && auth.Lockout.Duration > TimeSpan.Zero,
                $"'{AuthOptions.SectionName}' durations must be positive.")
            .ValidateOnStart();

        services.AddOptions<SeedAdminOptions>()
            .Bind(configuration.GetSection(SeedAdminOptions.SectionName));

        AddServices(services);

        return services;
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISeedAdminService, SeedAdminService>();
        services.AddScoped<IUserService, UserService>();
    }
}
