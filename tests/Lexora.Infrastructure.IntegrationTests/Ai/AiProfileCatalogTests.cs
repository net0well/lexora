using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Adapters;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.Fake;
using Lexora.Infrastructure.AI.Providers.Ollama;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class AiProfileCatalogTests
{
    private readonly AiProfileCatalog _catalog;

    public AiProfileCatalogTests()
    {
        var chat = new AiChatSettings
        {
            DefaultProfile = "local",
            Profiles =
            {
                ["fake"] = new AiProfileSettings { Provider = AiProviderKind.Fake, Model = "fake-chat" },
                ["claude"] = new AiProfileSettings { Provider = AiProviderKind.Anthropic, Model = "claude-sonnet-5-5", AllowUserSelection = true },
                ["local"] = new AiProfileSettings { Provider = AiProviderKind.Ollama, Model = "qwen2.5:7b", Endpoint = new Uri("http://localhost:11434"), AllowUserSelection = true },
                ["demo"] = new AiProfileSettings { Provider = AiProviderKind.Fake, Model = "demo-chat", AllowUserSelection = true },
            },
        };
        var registry = new ChatClientRegistry(
            [new OllamaChatClientProvider(), new FakeChatClientProvider()],
            Microsoft.Extensions.Options.Options.Create(new AiOptions { Chat = chat }),
            NullLoggerFactory.Instance);

        _catalog = new AiProfileCatalog(registry);
    }

    [Fact]
    public void ListChatProfiles_Configured_ListsTheDefaultFirstAndMarksWhatCanBeUsed() =>
        _catalog.ListChatProfiles().ShouldBe(
        [
            new AiChatProfile("local", "ollama", "qwen2.5:7b", Selectable: true, Available: true),
            new AiChatProfile("claude", "anthropic", "claude-sonnet-5-5", Selectable: false, Available: false),
            new AiChatProfile("demo", "fake", "demo-chat", Selectable: true, Available: true),
            new AiChatProfile("fake", "fake", "fake-chat", Selectable: false, Available: true),
        ]);

    [Theory]
    [InlineData("LOCAL", true)]
    [InlineData("claude", false)]
    [InlineData("fake", false)]
    [InlineData("gemini", false)]
    public void IsSelectable_Profile_NeedsToAllowSelectionAndBeAvailable(string name, bool expected) =>
        _catalog.IsSelectable(name).ShouldBe(expected);

    [Fact]
    public void DefaultChatProfile_IsTheConfiguredOne() => _catalog.DefaultChatProfile.ShouldBe("local");
}
