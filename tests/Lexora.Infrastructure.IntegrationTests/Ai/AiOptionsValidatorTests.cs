using Lexora.Infrastructure.AI.Options;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class AiOptionsValidatorTests
{
    private static readonly AiProfileSettings _fakeChat = new() { Provider = AiProviderKind.Fake, Model = "fake-chat" };

    private readonly AiOptionsValidator _validator = new();

    [Fact]
    public void Validate_CompleteOllamaProfile_Succeeds() =>
        _validator.Validate(null, With(new AiProfileSettings { Provider = AiProviderKind.Ollama, Model = "bge-m3", Endpoint = new Uri("http://ollama:11434") }))
            .Succeeded.ShouldBeTrue();

    [Fact]
    public void Validate_ActiveProfileMissing_Fails()
    {
        var options = new AiOptions { Chat = Chat(("fake", _fakeChat)), Embedding = new AiEmbeddingSettings { Profile = "missing" } };

        _validator.Validate(null, options).FailureMessage.ShouldNotBeNull().ShouldContain("'Ai:Embedding:Profile' must name one of the configured embedding profiles");
    }

    [Theory]
    [InlineData(AiProviderKind.Ollama, "bge-m3", null, null, "Endpoint")]
    [InlineData(AiProviderKind.OpenAI, "text-embedding-3-small", null, null, "ApiKey")]
    [InlineData(AiProviderKind.Anthropic, "any", null, "key", "Anthropic does not offer embeddings")]
    [InlineData(AiProviderKind.Fake, "", null, null, "Model")]
    public void Validate_IncompleteProfile_FailsNamingTheSetting(AiProviderKind provider, string model, string? endpoint, string? apiKey, string expected)
    {
        var profile = new AiProfileSettings { Provider = provider, Model = model, Endpoint = endpoint is null ? null : new Uri(endpoint), ApiKey = apiKey };

        _validator.Validate(null, With(profile)).FailureMessage.ShouldNotBeNull().ShouldContain(expected);
    }

    [Fact]
    public void Validate_NonPositiveNumbers_Fail()
    {
        var options = With(new AiProfileSettings { Provider = AiProviderKind.Fake, Model = "fake", MaxBatchSize = 0, TimeoutSeconds = 0 }, dimensions: 0);

        var failure = _validator.Validate(null, options).FailureMessage.ShouldNotBeNull();

        failure.ShouldContain("Dimensions");
        failure.ShouldContain("MaxBatchSize");
        failure.ShouldContain("TimeoutSeconds");
    }

    [Fact]
    public void Validate_DefaultChatProfileMissing_Fails()
    {
        var options = With(FakeEmbedding(), chat: new AiChatSettings { DefaultProfile = "local", Profiles = { ["fake"] = _fakeChat } });

        _validator.Validate(null, options).FailureMessage.ShouldNotBeNull()
            .ShouldContain("'Ai:Chat:DefaultProfile' must name one of the configured chat profiles (current: 'local')");
    }

    [Fact]
    public void Validate_DefaultChatProfileWithoutItsKey_FailsNamingTheKey()
    {
        var options = With(FakeEmbedding(), chat: Chat(("claude", new AiProfileSettings { Provider = AiProviderKind.Anthropic, Model = "claude-sonnet-5-5" })));

        _validator.Validate(null, options).FailureMessage.ShouldNotBeNull().ShouldContain("'Ai:Chat:Profiles:claude:ApiKey' is required.");
    }

    [Fact]
    public void Validate_OtherChatProfileWithoutItsKey_StillSucceeds()
    {
        var chat = Chat(("fake", _fakeChat), ("claude", new AiProfileSettings { Provider = AiProviderKind.Anthropic, Model = "claude-sonnet-5-5" }));

        _validator.Validate(null, With(FakeEmbedding(), chat: chat)).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_OtherChatProfileWithWrongShape_Fails()
    {
        var broken = new AiProfileSettings { Provider = (AiProviderKind)99, Model = "x", TimeoutSeconds = 0, Temperature = 3, MaxOutputTokens = 0 };

        var failure = _validator.Validate(null, With(FakeEmbedding(), chat: Chat(("fake", _fakeChat), ("broken", broken)))).FailureMessage.ShouldNotBeNull();

        failure.ShouldContain("'Ai:Chat:Profiles:broken:Provider' is not a known provider.");
        failure.ShouldContain("'Ai:Chat:Profiles:broken:TimeoutSeconds' must be positive.");
        failure.ShouldContain("'Ai:Chat:Profiles:broken:Temperature' must be between 0 and 2.");
        failure.ShouldContain("'Ai:Chat:Profiles:broken:MaxOutputTokens' must be positive.");
    }

    [Theory]
    [InlineData(AiProviderKind.Ollama, "http://ollama:11434", null, true)]
    [InlineData(AiProviderKind.Ollama, null, null, false)]
    [InlineData(AiProviderKind.OpenAI, null, "key", true)]
    [InlineData(AiProviderKind.OpenAI, null, null, false)]
    [InlineData(AiProviderKind.Anthropic, null, null, false)]
    [InlineData(AiProviderKind.OpenAICompatible, "https://api.groq.com/openai/v1", "key", true)]
    [InlineData(AiProviderKind.OpenAICompatible, "https://api.groq.com/openai/v1", null, false)]
    [InlineData(AiProviderKind.OpenAICompatible, null, "key", false)]
    [InlineData(AiProviderKind.Fake, null, null, true)]
    public void IsComplete_Profile_DependsOnWhatItsProviderNeeds(AiProviderKind provider, string? endpoint, string? apiKey, bool expected) =>
        AiProfileRequirements.IsComplete(new AiProfileSettings { Provider = provider, Model = "model", Endpoint = endpoint is null ? null : new Uri(endpoint), ApiKey = apiKey })
            .ShouldBe(expected);

    [Fact]
    public void IsComplete_LocalCompatibleServerWithoutKey_IsCompleteWhenItSaysItNeedsNone() =>
        AiProfileRequirements.IsComplete(new AiProfileSettings
        {
            Provider = AiProviderKind.OpenAICompatible,
            Model = "local-model",
            Endpoint = new Uri("http://localhost:1234/v1"),
            RequiresApiKey = false,
        }).ShouldBeTrue();

    private static AiProfileSettings FakeEmbedding() => new() { Provider = AiProviderKind.Fake, Model = "fake-embedding" };

    private static AiChatSettings Chat(params (string Name, AiProfileSettings Profile)[] profiles)
    {
        var chat = new AiChatSettings { DefaultProfile = profiles[0].Name };

        foreach (var (name, profile) in profiles)
        {
            chat.Profiles[name] = profile;
        }

        return chat;
    }

    private static AiOptions With(AiProfileSettings embedding, int dimensions = 1024, AiChatSettings? chat = null) =>
        new()
        {
            Chat = chat ?? Chat(("fake", _fakeChat)),
            Embedding = new AiEmbeddingSettings { Profile = "active", Dimensions = dimensions, Profiles = { ["active"] = embedding } },
        };
}
