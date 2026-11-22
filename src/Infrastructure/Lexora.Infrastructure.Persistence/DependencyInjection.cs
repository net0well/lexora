using Lexora.Domain.Abstractions;
using Lexora.Domain.ApiKeys;
using Lexora.Domain.Chunks;
using Lexora.Domain.Conversations;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence.Repositories;
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
