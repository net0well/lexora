using Lexora.Infrastructure.AI.Options;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class AiOptionsValidatorTests
{
    private readonly AiOptionsValidator _validator = new();

    [Fact]
    public void Validate_CompleteOllamaProfile_Succeeds() =>
        _validator.Validate(null, With(new AiProfileSettings { Provider = AiProviderKind.Ollama, Model = "bge-m3", Endpoint = new Uri("http://ollama:11434") }))
            .Succeeded.ShouldBeTrue();

    [Fact]
    public void Validate_ActiveProfileMissing_Fails()
    {
        var options = new AiOptions { Embedding = new AiEmbeddingSettings { Profile = "missing" } };

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

    private static AiOptions With(AiProfileSettings profile, int dimensions = 1024) =>
        new() { Embedding = new AiEmbeddingSettings { Profile = "active", Dimensions = dimensions, Profiles = { ["active"] = profile } } };
}
