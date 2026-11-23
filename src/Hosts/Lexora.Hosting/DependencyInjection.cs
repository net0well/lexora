using Lexora.Hosting.Health;
using Lexora.Hosting.Networking;
using Lexora.Hosting.Wolverine;
using Lexora.Shared.Configuration;
using Lexora.Shared.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lexora.Hosting;

public static class DependencyInjection
{
    public static IServiceCollection AddHostingDefaults(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        CodeGenerationDefaults.Apply(services, environment);
        ForwardedHeadersConfiguration.Apply(services, configuration);
        AddHealthChecks(services, configuration);

        return services;
    }

    private static void AddHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        var database = RequiredConnectionString(configuration, ConnectionStringNames.Database);
        var rabbitMq = RequiredConnectionString(configuration, ConnectionStringNames.RabbitMq);

        services.AddSingleton(new RabbitMqHealthConnection(new Uri(rabbitMq)));

        services.AddHealthChecks()
            .AddNpgSql(database, name: "postgresql", tags: [HealthCheckTags.Ready])
            .AddRabbitMQ(
                provider => provider.GetRequiredService<RabbitMqHealthConnection>().GetAsync(),
                name: "rabbitmq",
                tags: [HealthCheckTags.Ready]);
    }

    private static string RequiredConnectionString(IConfiguration configuration, string name)
    {
        var connectionString = configuration.GetConnectionString(name);

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException($"Connection string '{name}' not found.")
            : connectionString;
    }
}
