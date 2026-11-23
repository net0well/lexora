using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lexora.Infrastructure.AI.Health;

public sealed class EmbeddingProfileHealthCheck(EmbeddingGeneratorRegistry registry) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Paid providers are not called here: a probe every few seconds would cost money and quota.
        return registry.Profile.Provider == AiProviderKind.Ollama
            ? await OllamaModelProbe.CheckAsync(registry.ProfileName, registry.Profile, context.Registration.FailureStatus, cancellationToken)
            : HealthCheckResult.Healthy($"Embedding profile '{registry.ProfileName}' is configured.");
    }
}
