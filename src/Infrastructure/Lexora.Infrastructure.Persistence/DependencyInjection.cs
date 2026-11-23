using JasperFx.Core;
using Lexora.Application.Abstractions.Search;
using Lexora.Application.Conversations.Abstractions;
using Lexora.Application.Documents.Abstractions;
using Lexora.Application.KnowledgeBases.Abstractions;
using Lexora.Application.Users.Abstractions;
using Lexora.Domain.Abstractions;
using Lexora.Domain.ApiKeys;
using Lexora.Domain.Chunks;
using Lexora.Domain.Conversations;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence.Queries;
using Lexora.Infrastructure.Persistence.Repositories;
using Lexora.Infrastructure.Persistence.Search;
using Lexora.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
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

        AddDatabaseErrorPolicies(options);
        AddRepositories(options.Services);

        return options;
    }

    private static void AddDatabaseErrorPolicies(WolverineOptions options)
    {
        options.Policies.OnException<DbUpdateConcurrencyException>()
            .RetryWithCooldown(100.Milliseconds(), 250.Milliseconds(), 500.Milliseconds());

        options.Policies.OnException<NpgsqlException>(exception => exception.IsTransient)
            .RetryWithCooldown(500.Milliseconds(), 2.Seconds(), 5.Seconds())
            .Then.MoveToErrorQueue();
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

        services.AddScoped<IDocumentQueries, DocumentQueries>();
        services.AddScoped<IKnowledgeBaseQueries, KnowledgeBaseQueries>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IConversationQueries, ConversationQueries>();
        services.AddScoped<IChunkSearchIndex, ChunkSearchIndex>();
    }
}
