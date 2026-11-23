using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;
using Lexora.Domain.KnowledgeBases;
using Lexora.TestKit;
using NSubstitute;

namespace Lexora.Application.UnitTests.Ingestion;

public sealed class IngestionServiceChunkingTests
{
    private static readonly ExtractedDocument _extracted = new(
        [new ExtractedSection("Safety", 1, null, "Wear gloves."), new ExtractedSection(null, 2, null, "Turn machines off.")],
        PageCount: 2);

    private readonly IngestionHarness _harness = new();

    [Fact]
    public async Task ChunkAsync_ExtractedDocument_StoresChunksWithLocationAndMovesToChunked()
    {
        var document = await _harness.AddExtractedAsync(_extracted);

        await _harness.Service.ChunkAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.Status.ShouldBe(DocumentStatus.Chunked);
        document.ChunkCount.ShouldBe(2);
        document.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DocumentChunked>();
        _harness.Chunks.Chunks.Select(chunk => (chunk.Index, chunk.Content, chunk.Location.PageNumber, chunk.Location.SectionTitle))
            .ShouldBe([(0, "Wear gloves.", (int?)1, "Safety"), (1, "Turn machines off.", 2, null)]);
        _harness.Chunks.Chunks.ShouldAllBe(chunk => !chunk.HasEmbedding && chunk.ProcessingVersion == 1);
    }

    [Fact]
    public async Task ChunkAsync_KnowledgeBaseSettings_AreUsedByTheChunker()
    {
        var document = await _harness.AddExtractedAsync(_extracted);
        var knowledgeBase = await _harness.KnowledgeBases.GetByIdAsync(document.KnowledgeBaseId, TestContext.Current.CancellationToken);

        await _harness.Service.ChunkAsync(document.Id, 1, TestContext.Current.CancellationToken);

        _harness.Chunker.Received(1).Chunk(Arg.Any<ExtractedDocument>(), knowledgeBase!.Chunking);
    }

    [Fact]
    public async Task ChunkAsync_ReprocessedDocument_ReplacesTheChunksOfOtherVersions()
    {
        var document = await _harness.AddExtractedAsync(_extracted);
        await _harness.Chunks.AddRangeAsync(
            [DocumentChunk.Create(document.KnowledgeBaseId, document.Id, 7, 0, "old", 1, ChunkLocation.Create(1, null, null, 0, 3))],
            TestContext.Current.CancellationToken);

        await _harness.Service.ChunkAsync(document.Id, 1, TestContext.Current.CancellationToken);

        _harness.Chunks.Chunks.ShouldAllBe(chunk => chunk.ProcessingVersion == 1);
    }

    [Fact]
    public async Task ChunkAsync_ChunkerFindsNothing_FailsTheDocument()
    {
        _harness.Chunker.Chunk(Arg.Any<ExtractedDocument>(), Arg.Any<ChunkingSettings>()).Returns([]);
        var document = await _harness.AddExtractedAsync(_extracted);

        await _harness.Service.ChunkAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.Status.ShouldBe(DocumentStatus.Failed);
        document.FailureReason.ShouldBe(ExtractionFailures.NoText.Message);
    }

    [Fact]
    public async Task ChunkAsync_DocumentNotExtractedYet_IsIgnored()
    {
        var document = await _harness.AddUploadedAsync();

        await _harness.Service.ChunkAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.Status.ShouldBe(DocumentStatus.Uploaded);
        _harness.Chunks.Chunks.ShouldBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_ExtractedTextMissingFromStorage_FailsAsUnreadable()
    {
        var document = await _harness.AddUploadedAsync();
        document.CompleteExtraction(1, 1, "missing/extracted.json", TestClock.Now);

        await _harness.Service.ChunkAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.FailureReason.ShouldBe(ExtractionFailures.Unreadable.Message);
    }
}
