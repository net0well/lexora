using Lexora.Infrastructure.Persistence;
using Lexora.TestKit.Containers;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private static readonly string _truncateAllTables =
        "TRUNCATE " + string.Join(", ", DomainTables().Select(table => $"{SchemaHelper.Schema}.{table}")) + " CASCADE";

    private readonly PostgreSqlContainer _container = PostgreSqlContainerFactory.Create();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync(TestContext.Current.CancellationToken);

        await using var context = CreateContext();
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public LexoraDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<LexoraDbContext>();
        DbContextConfiguration.Configure(builder, ConnectionString);

        return new LexoraDbContext(builder.Options);
    }

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(_truncateAllTables, cancellationToken);
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();

    private static IEnumerable<string> DomainTables() =>
    [
        SchemaHelper.Users,
        SchemaHelper.RefreshTokens,
        SchemaHelper.ApiKeys,
        SchemaHelper.KnowledgeBases,
        SchemaHelper.Documents,
        SchemaHelper.DocumentChunks,
        SchemaHelper.Conversations,
        SchemaHelper.ConversationMessages,
    ];
}
