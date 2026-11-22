using Lexora.Domain.Abstractions;
using Lexora.Domain.ApiKeys;
using Lexora.Domain.Chunks;
using Lexora.Domain.Conversations;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence.Repositories;
using Lexora.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence;
using Wolverine.Postgresql;

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

        var persistence = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>();

        if (string.IsNullOrWhiteSpace(persistence?.WolverineSchema))
        {
            throw new InvalidOperationException($"Setting '{PersistenceOptions.SectionName}:{nameof(PersistenceOptions.WolverineSchema)}' not found.");
        }

        options.Services.AddDbContextWithWolverineIntegration<LexoraDbContext>(
            builder => DbContextConfiguration.Configure(builder, connectionString));

        options.Services.AddScoped<IUnitOfWork>(services => services.GetRequiredService<LexoraDbContext>());

        options.PersistMessagesWithPostgresql(connectionString, persistence.WolverineSchema);

        options.UseEntityFrameworkCoreTransactions(TransactionMiddlewareMode.Lightweight)
            .WithDbContextAbstraction<IUnitOfWork, LexoraDbContext>();

        options.PublishDomainEventsFromEntityFrameworkCore<AggregateRoot>(aggregate => aggregate.DomainEvents);

        AddRepositories(options.Services);

        return options;
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IKnowledgeBaseRepository, KnowledgeBaseRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
    }
}
