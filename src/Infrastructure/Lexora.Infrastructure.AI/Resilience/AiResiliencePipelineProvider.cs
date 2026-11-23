using System.Collections.Concurrent;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Lexora.Infrastructure.AI.Resilience;

public sealed class AiResiliencePipelineProvider(TimeProvider timeProvider)
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan LongestHonoredRetryAfter = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(30);
    public const int MinimumThroughput = 5;

    private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines = new(StringComparer.OrdinalIgnoreCase);

    public ResiliencePipeline ForChat(string profileName) => _pipelines.GetOrAdd($"chat:{profileName}", _ => Build());

    public ResiliencePipeline ForEmbedding(string profileName) => _pipelines.GetOrAdd($"embedding:{profileName}", _ => Build());

    private ResiliencePipeline Build() =>
        new ResiliencePipelineBuilder { TimeProvider = timeProvider }
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 1,
                Delay = RetryDelay,
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = arguments => ValueTask.FromResult(ShouldRetry(arguments.Outcome.Exception, arguments.Context.CancellationToken)),
                DelayGenerator = arguments => ValueTask.FromResult(
                    arguments.Outcome.Exception is { } exception ? AiFailureClassifier.RetryAfterOf(exception) : null),
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = MinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = BreakDuration,
                ShouldHandle = arguments => ValueTask.FromResult(
                    arguments.Outcome.Exception is { } exception && AiFailureClassifier.IsTransient(exception, arguments.Context.CancellationToken)),
            })
            .Build();

    // A provider asking for a long pause is better served by the Wolverine schedule than by holding the call.
    private static bool ShouldRetry(Exception? exception, CancellationToken cancellationToken) =>
        exception is not null
        && AiFailureClassifier.IsTransient(exception, cancellationToken)
        && !(AiFailureClassifier.RetryAfterOf(exception) > LongestHonoredRetryAfter);
}
