using Lexora.Application.Abstractions.Storage;
using Lexora.Infrastructure.Storage.Local;
using Lexora.Infrastructure.Storage.S3;
using Lexora.Shared.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddStorageAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(StorageOptions.SectionName);

        services.AddOptions<StorageOptions>()
            .Bind(section)
            .Validate(storage => Enum.IsDefined(storage.Provider), $"'{StorageOptions.SectionName}:Provider' must be S3 or Local.")
            .Validate(storage => storage.DownloadUrlLifetime > TimeSpan.Zero, $"'{StorageOptions.SectionName}:DownloadUrlLifetime' must be positive.")
            .ValidateOnStart();

        services.AddDataProtection();
        services.AddSingleton<LocalDownloadTickets>();

        switch (section.Get<StorageOptions>()?.Provider ?? StorageProvider.S3)
        {
            case StorageProvider.Local:
                AddLocal(services, configuration);
                break;
            default:
                AddS3(services, configuration);
                break;
        }

        return services;
    }

    private static void AddS3(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(S3StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<S3ClientFactory>();
        services.AddSingleton<IFileStorage, S3FileStorage>();

        services.AddHealthChecks().AddCheck<S3StorageHealthCheck>("storage", tags: [HealthCheckTags.Ready]);
    }

    private static void AddLocal(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LocalStorageOptions>()
            .Bind(configuration.GetSection(LocalStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                local => local.DownloadBaseUrl is { IsAbsoluteUri: true } url && url.AbsolutePath.EndsWith('/'),
                $"'{LocalStorageOptions.SectionName}:DownloadBaseUrl' must be an absolute URL ending with '/'.")
            .ValidateOnStart();

        services.AddSingleton<IFileStorage, LocalFileStorage>();
    }
}
