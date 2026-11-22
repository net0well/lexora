using Lexora.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence;

public sealed class LexoraDbContext(DbContextOptions<LexoraDbContext> options) : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LexoraDbContext).Assembly);
}
