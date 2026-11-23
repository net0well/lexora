using FluentValidation;
using Lexora.Application.Auth.SeedAdmin;
using Lexora.Application.Auth.Services;
using Lexora.Application.Common.Options;
using Lexora.Application.Conversations.Prompts;
using Lexora.Application.Conversations.Services;
using Lexora.Application.Documents.Services;
using Lexora.Application.Ingestion.Services;
using Lexora.Application.KnowledgeBases.Services;
using Lexora.Application.Retrieval.Fusion;
using Lexora.Application.Retrieval.Services;
using Lexora.Application.Users.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                auth => auth.RefreshTokenLifetime > TimeSpan.Zero && auth.Lockout.Duration > TimeSpan.Zero,
                $"'{AuthOptions.SectionName}' durations must be positive.")
            .ValidateOnStart();

        services.AddOptions<SeedAdminOptions>()
            .Bind(configuration.GetSection(SeedAdminOptions.SectionName));

        services.AddOptions<RagOptions>()
            .Bind(configuration.GetSection(RagOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<IngestionOptions>()
            .Bind(configuration.GetSection(IngestionOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(ingestion => ingestion.StalledAfter > TimeSpan.Zero, $"'{IngestionOptions.SectionName}:StalledAfter' must be positive.")
            .ValidateOnStart();

        services.AddOptions<DocumentOptions>()
            .Bind(configuration.GetSection(DocumentOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                documents => documents.MaxFileSizeBytes <= documents.MaxTotalStorageBytes,
                $"'{DocumentOptions.SectionName}:MaxFileSizeBytes' cannot exceed 'MaxTotalStorageBytes'.")
            .ValidateOnStart();

        AddServices(services);

        return services;
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISeedAdminService, SeedAdminService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IIngestionService, IngestionService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddSingleton<IRankFusion, ReciprocalRankFusion>();
        services.AddScoped<IRetrievalService, RetrievalService>();
        services.AddSingleton<IRagPromptBuilder, RagPromptBuilder>();
        services.AddSingleton<ICitationParser, CitationParser>();
        services.AddScoped<IFollowUpQuestionRewriter, FollowUpQuestionRewriter>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IAnswerService, AnswerService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<IUserService, UserService>();
    }
}
