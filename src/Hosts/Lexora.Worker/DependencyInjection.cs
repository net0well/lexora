using Lexora.Worker.Scheduling;

namespace Lexora.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MaintenanceSchedulerOptions>()
            .Bind(configuration.GetSection(MaintenanceSchedulerOptions.SectionName))
            .Validate(
                scheduler => scheduler.StalledDocumentsInterval > TimeSpan.Zero
                    && scheduler.OrphanFilesInterval > TimeSpan.Zero
                    && scheduler.ExpiredRefreshTokensInterval > TimeSpan.Zero,
                $"'{MaintenanceSchedulerOptions.SectionName}' intervals must be positive.")
            .ValidateOnStart();

        services.AddHostedService<MaintenanceScheduler>();

        return services;
    }
}
