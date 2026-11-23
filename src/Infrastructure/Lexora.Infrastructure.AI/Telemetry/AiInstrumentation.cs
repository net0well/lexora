using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Lexora.Infrastructure.AI.Telemetry;

public static class AiInstrumentation
{
    public const string SourceName = "Lexora.AI";

    public static IChatClient Instrument(IChatClient client, AiOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new ChatClientBuilder(client)
            .UseOpenTelemetry(loggerFactory, SourceName, telemetry => telemetry.EnableSensitiveData = false);

        // The detailed client log carries prompts and answers, so it only runs when explicitly asked for.
        return options.LogPrompts ? builder.UseLogging(loggerFactory).Build() : builder.Build();
    }

    public static IEmbeddingGenerator<string, Embedding<float>> Instrument(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        AiOptions options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new EmbeddingGeneratorBuilder<string, Embedding<float>>(generator)
            .UseOpenTelemetry(loggerFactory, SourceName, telemetry => telemetry.EnableSensitiveData = false);

        return options.LogPrompts ? builder.UseLogging(loggerFactory).Build() : builder.Build();
    }
}
