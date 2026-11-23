using Lexora.Shared.Guards;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence;

public static class DbContextConfiguration
{
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString)
    {
        Guard.NotNull(builder);
        Guard.NotNullOrWhiteSpace(connectionString);

        return builder
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseVector();
                npgsql.MigrationsHistoryTable(SchemaHelper.MigrationsHistoryTable, SchemaHelper.Schema);
            })
            .UseSnakeCaseNamingConvention();
    }
}
