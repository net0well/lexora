using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Ai.ListAiProfiles;
using Lexora.TestKit.Fakes;
using NSubstitute;

namespace Lexora.Application.UnitTests.Ai;

public sealed class ListAiProfilesHandlerTests
{
    [Fact]
    public void Handle_ConfiguredProfiles_DescribesChatProfilesAndTheEmbeddingSpace()
    {
        var catalog = Substitute.For<IAiProfileCatalog>();
        catalog.DefaultChatProfile.Returns("local");
        catalog.ListChatProfiles().Returns(
        [
            new AiChatProfile("local", "ollama", "qwen2.5:7b", Selectable: true, Available: true),
            new AiChatProfile("claude", "anthropic", "claude-sonnet-5-5", Selectable: false, Available: false),
        ]);

        var response = ListAiProfilesHandler.Handle(new ListAiProfilesQuery(), catalog, new FakeEmbeddingModel(dimensions: 1024)).Value;

        response.DefaultChatProfile.ShouldBe("local");
        response.ChatProfiles.ShouldBe(
        [
            new AiChatProfileResponse("local", "ollama", "qwen2.5:7b", Selectable: true, Available: true),
            new AiChatProfileResponse("claude", "anthropic", "claude-sonnet-5-5", Selectable: false, Available: false),
        ]);
        response.Embedding.ShouldBe(new AiEmbeddingProfileResponse("fake", "fake-embedding", 1024));
    }
}
