using System.Net.Http.Json;
using System.Text.Json;
using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lexora.Infrastructure.AI.Health;

public static class OllamaModelProbe
{
    // Shared across checks: readiness probes run every few seconds and must not churn sockets.
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    public static async Task<HealthCheckResult> CheckAsync(
        string profileName,
        AiProfileSettings profile,
        HealthStatus failureStatus,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        try
        {
            var tags = await _http.GetFromJsonAsync<JsonElement>(new Uri(profile.Endpoint!, "api/tags"), cancellationToken);
            var pulled = tags.GetProperty("models").EnumerateArray()
                .Select(model => model.GetProperty("name").GetString())
                .Any(name => name == profile.Model || name == $"{profile.Model}:latest");

            return pulled
                ? HealthCheckResult.Healthy($"Ollama serves '{profile.Model}' for profile '{profileName}'.")
                : new HealthCheckResult(failureStatus, $"Ollama does not have the model '{profile.Model}' of profile '{profileName}'.");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException
            && !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(failureStatus, $"Ollama is not answering for profile '{profileName}'.", exception);
        }
    }
}
