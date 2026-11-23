using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Adapters;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers;
using Lexora.Infrastructure.AI.Providers.Fake;
using Lexora.Infrastructure.AI.Providers.Ollama;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAiAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AiOptions>, AiOptionsValidator>();

        services.AddSingleton<IChatClientProvider, OllamaChatClientProvider>();
        services.AddSingleton<IEmbeddingGeneratorProvider, OllamaEmbeddingGeneratorProvider>();
        services.AddSingleton<IEmbeddingGeneratorProvider, HashingEmbeddingGeneratorProvider>();
        services.AddSingleton<ChatClientRegistry>();
        services.AddSingleton<EmbeddingGeneratorRegistry>();
        services.AddSingleton<IChatModel, ChatModelAdapter>();
        services.AddSingleton<IEmbeddingModel, EmbeddingModelAdapter>();

        return services;
    }
}
