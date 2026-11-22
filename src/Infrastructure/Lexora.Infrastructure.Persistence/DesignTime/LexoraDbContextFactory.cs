using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lexora.Infrastructure.Persistence.DesignTime;

public sealed class LexoraDbContextFactory : IDesignTimeDbContextFactory<LexoraDbContext>
{
    public const string ConnectionStringVariable = "LEXORA_DESIGN_CONNECTION";

    private const string LocalConnectionString = "Host=localhost;Port=5432;Database=lexora;Username=lexora;Password=lexora";

    public LexoraDbContext CreateDbContext(string[] args)
    {
        var configured = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        var connectionString = string.IsNullOrWhiteSpace(configured) ? LocalConnectionString : configured;

        var builder = new DbContextOptionsBuilder<LexoraDbContext>();
        DbContextConfiguration.Configure(builder, connectionString);

        return new LexoraDbContext(builder.Options);
    }
}
