using Lexora.Application.Abstractions.Ai;

namespace Lexora.Application.UnitTests.Abstractions;

public sealed class EmbeddingSpaceTests
{
    [Fact]
    public void Key_Space_CombinesProviderModelAndDimensionsInLowerCase() =>
        new EmbeddingSpace("Ollama", "BGE-M3", 1024).Key.ShouldBe("ollama:bge-m3:1024");

    [Fact]
    public void Key_DifferentDimensions_AreDifferentSpaces() =>
        new EmbeddingSpace("openai", "text-embedding-3-small", 1024).Key.ShouldNotBe(new EmbeddingSpace("openai", "text-embedding-3-small", 1536).Key);

    [Fact]
    public void Unavailable_ProviderDetails_AreKeptForRetryPolicies()
    {
        var exception = new AiProviderUnavailableException("ollama", "local", TimeSpan.FromSeconds(3), new HttpRequestException("503"));

        (exception.Provider, exception.Profile, exception.RetryAfter).ShouldBe(("ollama", "local", TimeSpan.FromSeconds(3)));
        exception.Message.ShouldContain("ollama");
    }
}
