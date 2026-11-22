using FluentValidation;
using Lexora.Application.Auth.SeedAdmin;
using Lexora.Application.Auth.Services;
using Lexora.Application.Common.Options;
using Lexora.Application.Documents.Services;
using Lexora.Application.KnowledgeBases.Services;
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

        services.AddOptions<DocumentOptions>()
            .Bind(configuration.GetSection(DocumentOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                documents => documents.MaxFileSizeBytes <= documents.MaxTotalStorageBytes,
                $"'{DocumentOptions.SectionName}:MaxFileSizeBytes' cannot exceed 'MaxTotalStorageBytes'.")
            .ValidateOnStart();

        AddServices(services);

        return services;
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISeedAdminService, SeedAdminService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<IUserService, UserService>();
    }
}
