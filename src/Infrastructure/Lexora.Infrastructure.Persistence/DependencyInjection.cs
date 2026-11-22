using Lexora.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Lexora.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static WolverineOptions AddPersistenceAdapter(this WolverineOptions options, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(PersistenceOptions.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{PersistenceOptions.ConnectionStringName}' not found.");
        }

        options.Services.AddDbContext<LexoraDbContext>(
            builder => DbContextConfiguration.Configure(builder, connectionString),
            optionsLifetime: ServiceLifetime.Singleton);

        options.Services.AddScoped<IUnitOfWork>(services => services.GetRequiredService<LexoraDbContext>());

        return options;
    }
}
