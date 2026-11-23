using System.Security.Cryptography;
using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.CommandLine;
using JasperFx.Resources;
using Lexora.Infrastructure.IntegrationTests.Persistence;
using Lexora.Shared.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Wolverine;

namespace Lexora.Infrastructure.IntegrationTests.Worker;

public sealed class WorkerFactory(DatabaseFixture database) : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "lexora-worker-tests", Guid.CreateVersion7().ToString("N"));

    public string? RabbitMqConnectionString { get; init; }

    public ValueTask InitializeAsync()
    {
        JasperFxEnvironment.AutoStartHost = true;
        StartServer();

        return ValueTask.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{ConnectionStringNames.Database}", database.ConnectionString);
        builder.UseSetting($"ConnectionStrings:{ConnectionStringNames.RabbitMq}", RabbitMqConnectionString ?? "amqp://guest:guest@localhost:5672");
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        builder.UseSetting("Ai:Chat:DefaultProfile", "fake");
        builder.UseSetting("Ai:Embedding:Profile", "fake");
        builder.UseSetting("Storage:Provider", "Local");
        builder.UseSetting("Storage:Local:RootPath", StorageRoot);
        builder.UseSetting("Storage:Local:DownloadBaseUrl", "https://localhost/api/v1/files/");

        builder.ConfigureServices(services =>
        {
            services.CritterStackDefaults(defaults =>
            {
                defaults.Production.GeneratedCodeMode = TypeLoadMode.Dynamic;
                defaults.Production.AssertAllPreGeneratedTypesExist = false;
                defaults.Production.ResourceAutoCreate = AutoCreate.CreateOrUpdate;
            });

            if (RabbitMqConnectionString is null)
            {
                services.DisableAllExternalWolverineTransports();
            }

            services.AddResourceSetupOnStartup();
        });
    }
}
