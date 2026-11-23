using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Adapters;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers;
using Lexora.Infrastructure.AI.Providers.Anthropic;
using Lexora.Infrastructure.AI.Providers.Fake;
using Lexora.Infrastructure.AI.Providers.Ollama;
using Lexora.Infrastructure.AI.Providers.OpenAi;
using Lexora.Infrastructure.AI.Providers.OpenAiCompatible;
using Lexora.Infrastructure.AI.Registry;
using Lexora.Infrastructure.AI.Resilience;
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
        services.AddSingleton<IChatClientProvider, OpenAiChatClientProvider>();
        services.AddSingleton<IChatClientProvider, OpenAiCompatibleChatClientProvider>();
        services.AddSingleton<IChatClientProvider, AnthropicChatClientProvider>();
        services.AddSingleton<IChatClientProvider, FakeChatClientProvider>();
        services.AddSingleton<IEmbeddingGeneratorProvider, OllamaEmbeddingGeneratorProvider>();
        services.AddSingleton<IEmbeddingGeneratorProvider, OpenAiEmbeddingGeneratorProvider>();
        services.AddSingleton<IEmbeddingGeneratorProvider, OpenAiCompatibleEmbeddingGeneratorProvider>();
        services.AddSingleton<IEmbeddingGeneratorProvider, HashingEmbeddingGeneratorProvider>();
        services.AddSingleton<AiResiliencePipelineProvider>();
        services.AddSingleton<ChatClientRegistry>();
        services.AddSingleton<EmbeddingGeneratorRegistry>();
        services.AddSingleton<IChatModel, ChatModelAdapter>();
        services.AddSingleton<IEmbeddingModel, EmbeddingModelAdapter>();

        return services;
    }
}
