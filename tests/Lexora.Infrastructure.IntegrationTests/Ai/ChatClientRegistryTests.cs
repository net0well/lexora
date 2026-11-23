using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Registry;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class ChatClientRegistryTests
{
    private readonly StubChatClientProvider _ollama = new(AiProviderKind.Ollama);
    private readonly StubChatClientProvider _fake = new(AiProviderKind.Fake);

    [Fact]
    public void Resolve_NoProfile_UsesTheDefaultOne()
    {
        using var registry = Registry();

        var resolved = registry.Resolve(profileName: null);

        resolved.ProfileName.ShouldBe("local");
        resolved.Profile.Model.ShouldBe("qwen2.5:7b");
        resolved.Client.ShouldBeSameAs(_ollama.Created.ShouldHaveSingleItem().Client);
    }

    [Fact]
    public void Resolve_SameProfileTwice_CreatesItsClientOnce()
    {
        using var registry = Registry();

        var first = registry.Resolve("FAKE");
        var second = registry.Resolve("fake");

        second.Client.ShouldBeSameAs(first.Client);
        _fake.Created.ShouldHaveSingleItem().ProfileName.ShouldBe("FAKE");
    }

    [Fact]
    public void Resolve_UnknownProfile_Throws()
    {
        using var registry = Registry();

        Should.Throw<InvalidOperationException>(() => registry.Resolve("gemini")).Message.ShouldBe("Unknown chat profile 'gemini'.");
    }

    [Theory]
    [InlineData("claude")]
    [InlineData("lmstudio")]
    public void Resolve_ProfileThatCannotWork_ThrowsAndIsNotAvailable(string profileName)
    {
        using var registry = Registry();

        registry.IsAvailable(profileName).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => registry.Resolve(profileName)).Message.ShouldContain($"Chat profile '{profileName}' is not available");
    }

    [Fact]
    public void Dispose_CreatedClients_AreDisposed()
    {
        var registry = Registry();
        registry.Resolve("local");

        registry.Dispose();

        _ollama.Created.ShouldHaveSingleItem().Client.Disposed.ShouldBeTrue();
        _fake.Created.ShouldBeEmpty();
    }

    private ChatClientRegistry Registry()
    {
        var chat = new AiChatSettings
        {
            DefaultProfile = "local",
            Profiles =
            {
                ["local"] = new AiProfileSettings { Provider = AiProviderKind.Ollama, Model = "qwen2.5:7b", Endpoint = new Uri("http://localhost:11434") },
                ["fake"] = new AiProfileSettings { Provider = AiProviderKind.Fake, Model = "fake-chat" },
                ["claude"] = new AiProfileSettings { Provider = AiProviderKind.Anthropic, Model = "claude-sonnet-5-5" },
                ["lmstudio"] = new AiProfileSettings { Provider = AiProviderKind.OpenAICompatible, Model = "local-model", Endpoint = new Uri("http://localhost:1234/v1") },
            },
        };

        return new ChatClientRegistry([_ollama, _fake], Microsoft.Extensions.Options.Options.Create(new AiOptions { Chat = chat }));
    }
}
