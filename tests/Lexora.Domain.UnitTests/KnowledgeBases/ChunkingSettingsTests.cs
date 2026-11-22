using Lexora.Domain.KnowledgeBases;

namespace Lexora.Domain.UnitTests.KnowledgeBases;

public sealed class ChunkingSettingsTests
{
    [Fact]
    public void Default_Settings_Are800TokensWith120Overlap()
    {
        ChunkingSettings.Default.MaxTokens.ShouldBe(800);
        ChunkingSettings.Default.OverlapTokens.ShouldBe(120);
    }

    [Theory]
    [InlineData(200, 0)]
    [InlineData(200, 100)]
    [InlineData(800, 120)]
    [InlineData(2000, 1000)]
    public void Create_WithinBounds_Succeeds(int maxTokens, int overlapTokens)
    {
        var result = ChunkingSettings.Create(maxTokens, overlapTokens);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MaxTokens.ShouldBe(maxTokens);
        result.Value.OverlapTokens.ShouldBe(overlapTokens);
    }

    [Theory]
    [InlineData(199, 0)]
    [InlineData(2001, 0)]
    [InlineData(800, -1)]
    [InlineData(800, 401)]
    [InlineData(201, 101)]
    public void Create_OutOfBounds_FailsWithChunkingInvalid(int maxTokens, int overlapTokens) =>
        ChunkingSettings.Create(maxTokens, overlapTokens).Notifications.Single().Key.ShouldBe("knowledgeBase.chunking.invalid");

    [Fact]
    public void Equals_SameValues_ReturnsTrue() => ChunkingSettings.Create(800, 120).Value.ShouldBe(ChunkingSettings.Default);
}
