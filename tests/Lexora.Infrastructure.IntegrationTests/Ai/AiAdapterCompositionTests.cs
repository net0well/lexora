using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class AiAdapterCompositionTests
{
    private static readonly string _apiSettings = Path.Combine(RepositoryRoot(), "src", "Hosts", "Lexora.Api", "appsettings.json");

    [Fact]
    public void ShippedSettings_WithoutKeys_StartWithLocalModelsAndListPaidProfilesAsUnavailable()
    {
        using var services = Compose();

        services.GetRequiredService<IOptions<AiOptions>>().Value.Chat.DefaultProfile.ShouldBe("local");
        services.GetRequiredService<IAiProfileCatalog>().ListChatProfiles()
            .Where(profile => profile.Available)
            .Select(profile => profile.Name)
            .ShouldBe(["local", "fake"]);
        services.GetRequiredService<IEmbeddingModel>().Space.Key.ShouldBe("ollama:bge-m3:1024");
    }

    [Theory]
    [InlineData("local", "ollama", "localhost")]
    [InlineData("claude", "anthropic", "api.anthropic.com")]
    [InlineData("openai", "openai", "api.openai.com")]
    [InlineData("groq", "openai", "api.groq.com")]
    [InlineData("fake", "fake", null)]
    public void DefaultChatProfile_ChangedInConfiguration_SelectsItsProvider(string profile, string providerName, string? host)
    {
        using var services = Compose(WithKeys(("Ai:Chat:DefaultProfile", profile)));

        var metadata = services.GetRequiredService<ChatClientRegistry>().Resolve(null).Client.GetService<ChatClientMetadata>().ShouldNotBeNull();

        metadata.ProviderName.ShouldBe(providerName);
        metadata.ProviderUri?.Host.ShouldBe(host);
        services.GetRequiredService<IAiProfileCatalog>().DefaultChatProfile.ShouldBe(profile);
    }

    [Theory]
    [InlineData("local", "ollama:bge-m3:1024")]
    [InlineData("openai", "openai:text-embedding-3-small:1024")]
    [InlineData("fake", "fake:fake-embedding:1024")]
    public void EmbeddingProfile_ChangedInConfiguration_DefinesTheEmbeddingSpace(string profile, string expectedKey)
    {
        using var services = Compose(WithKeys(("Ai:Embedding:Profile", profile)));

        services.GetRequiredService<IEmbeddingModel>().Space.Key.ShouldBe(expectedKey);
    }

    [Theory]
    [InlineData("Ai:Chat:DefaultProfile", "claude", "'Ai:Chat:Profiles:claude:ApiKey' is required.")]
    [InlineData("Ai:Chat:DefaultProfile", "groq", "'Ai:Chat:Profiles:groq:ApiKey' is required.")]
    [InlineData("Ai:Embedding:Profile", "openai", "'Ai:Embedding:Profiles:openai:ApiKey' is required.")]
    [InlineData("Ai:Chat:DefaultProfile", "gemini", "'Ai:Chat:DefaultProfile' must name one of the configured chat profiles (current: 'gemini').")]
    public void ActiveProfileThatCannotWork_StopsTheHostNamingTheSetting(string key, string value, string expected)
    {
        using var services = Compose((key, value));

        Should.Throw<OptionsValidationException>(() => services.GetRequiredService<IOptions<AiOptions>>().Value).Message.ShouldContain(expected);
    }

    private static (string Key, string Value)[] WithKeys(params (string Key, string Value)[] overrides) =>
    [
        ("Ai:Chat:Profiles:claude:ApiKey", "sk-ant-test"),
        ("Ai:Chat:Profiles:openai:ApiKey", "sk-test"),
        ("Ai:Chat:Profiles:groq:ApiKey", "gsk-test"),
        ("Ai:Embedding:Profiles:openai:ApiKey", "sk-test"),
        .. overrides,
    ];

    private static ServiceProvider Compose(params (string Key, string Value)[] overrides)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(_apiSettings)
            .AddInMemoryCollection(overrides.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddSingleton(TimeProvider.System)
            .AddAiAdapter(configuration)
            .BuildServiceProvider();
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
