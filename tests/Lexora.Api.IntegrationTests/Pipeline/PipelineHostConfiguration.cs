using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Resources;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Shared.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace Lexora.Api.IntegrationTests.Pipeline;

public static class PipelineHostConfiguration
{
    public static void Apply(IWebHostBuilder builder, PipelineSettings settings)
    {
        builder.UseEnvironment(ApiFactory.EnvironmentName);
        builder.UseSetting($"ConnectionStrings:{ConnectionStringNames.Database}", settings.DatabaseConnectionString);
        builder.UseSetting($"ConnectionStrings:{ConnectionStringNames.RabbitMq}", settings.RabbitMqConnectionString);
        builder.UseSetting("Jwt:SigningKey", settings.SigningKey);
        builder.UseSetting("Ai:Chat:DefaultProfile", "fake");
        builder.UseSetting("Ai:Embedding:Profile", "fake");
        builder.UseSetting("Storage:Provider", "Local");
        builder.UseSetting("Storage:Local:RootPath", settings.StorageRoot);
        builder.UseSetting("Storage:Local:DownloadBaseUrl", "https://localhost/api/v1/files/");

        builder.ConfigureServices(services =>
        {
            services.CritterStackDefaults(defaults =>
            {
                defaults.Production.GeneratedCodeMode = TypeLoadMode.Dynamic;
                defaults.Production.AssertAllPreGeneratedTypesExist = false;
                defaults.Production.ResourceAutoCreate = AutoCreate.CreateOrUpdate;
            });

            services.AddResourceSetupOnStartup();
        });
    }
}
