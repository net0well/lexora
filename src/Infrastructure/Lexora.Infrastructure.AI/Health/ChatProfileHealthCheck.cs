using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lexora.Infrastructure.AI.Health;

public sealed class ChatProfileHealthCheck(ChatClientRegistry registry) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var name = registry.DefaultProfile;

        if (!registry.IsAvailable(name))
        {
            return new HealthCheckResult(context.Registration.FailureStatus, $"Chat profile '{name}' is not usable.");
        }

        var profile = registry.Profiles[name];

        // Paid providers are not called here: a probe every few seconds would cost money and quota.
        return profile.Provider == AiProviderKind.Ollama
            ? await OllamaModelProbe.CheckAsync(name, profile, context.Registration.FailureStatus, cancellationToken)
            : HealthCheckResult.Healthy($"Chat profile '{name}' is configured.");
    }
}
