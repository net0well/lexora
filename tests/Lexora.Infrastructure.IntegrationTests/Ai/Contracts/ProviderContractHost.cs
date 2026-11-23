using System.Net.Http.Json;
using System.Text.Json;
using Lexora.Infrastructure.AI;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.IntegrationTests.Ai.Contracts;

// Real provider keys come from the user secrets of this project or the environment, never from committed settings.
public sealed class ProviderContractHost : IDisposable
{
    private readonly ServiceProvider _services;

    public ProviderContractHost(params (string Key, string Value)[] overrides)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(RepositoryRoot(), "src", "Hosts", "Lexora.Api", "appsettings.json"))
            .AddInMemoryCollection(
            [
                new("Ai:Chat:DefaultProfile", "fake"),
                new("Ai:Embedding:Profile", "fake"),
                new("Ai:Chat:Profiles:local:TimeoutSeconds", "300"),
                new("Ai:Embedding:Profiles:local:TimeoutSeconds", "300"),
            ])
            .AddUserSecrets<ProviderContractHost>(optional: true)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(overrides.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(TimeProvider.System)
            .AddAiAdapter(configuration)
            .BuildServiceProvider();
    }

    public IServiceProvider Services => _services;

    public AiOptions Options => _services.GetRequiredService<IOptions<AiOptions>>().Value;

    public async Task SkipUnlessChatProfileWorksAsync(string profileName)
    {
        if (!_services.GetRequiredService<ChatClientRegistry>().IsAvailable(profileName))
        {
            Assert.Skip($"Chat profile '{profileName}' has no API key; set 'Ai:Chat:Profiles:{profileName}:ApiKey' in this project's user secrets to run it.");
        }

        if (Options.Chat.Profiles[profileName] is { Provider: AiProviderKind.Ollama } ollama)
        {
            await SkipUnlessOllamaServesAsync(ollama);
        }
    }

    // Checked on a host that still embeds with the fake profile, since activating an incomplete profile fails validation.
    public static async Task SkipUnlessEmbeddingProfileWorksAsync(string profileName)
    {
        using var probe = new ProviderContractHost();
        var profile = probe.Options.Embedding.Profiles[profileName];

        if (!AiProfileRequirements.IsComplete(profile))
        {
            Assert.Skip($"Embedding profile '{profileName}' has no API key; set 'Ai:Embedding:Profiles:{profileName}:ApiKey' in this project's user secrets to run it.");
        }

        if (profile.Provider == AiProviderKind.Ollama)
        {
            await SkipUnlessOllamaServesAsync(profile);
        }
    }

    public void Dispose() => _services.Dispose();

    private static async Task SkipUnlessOllamaServesAsync(AiProfileSettings profile)
    {
        using var http = new HttpClient { BaseAddress = profile.Endpoint, Timeout = TimeSpan.FromSeconds(5) };
        JsonElement tags;

        try
        {
            tags = await http.GetFromJsonAsync<JsonElement>("api/tags", TestContext.Current.CancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            Assert.Skip($"Ollama is not answering at {profile.Endpoint}; start it with 'docker compose --profile ai-local up -d ollama'.");
            return;
        }

        var models = tags.GetProperty("models").EnumerateArray().Select(model => model.GetProperty("name").GetString()).ToList();

        if (!models.Any(model => model == profile.Model || model == $"{profile.Model}:latest"))
        {
            Assert.Skip($"Ollama does not have '{profile.Model}'; pull it with 'ollama pull {profile.Model}'.");
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lexora.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }
}
