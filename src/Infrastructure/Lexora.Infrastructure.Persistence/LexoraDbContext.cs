using Lexora.Domain.Abstractions;
using Lexora.Domain.ApiKeys;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence;

public sealed class LexoraDbContext(DbContextOptions<LexoraDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();

    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaHelper.Schema);
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LexoraDbContext).Assembly);
    }
}
