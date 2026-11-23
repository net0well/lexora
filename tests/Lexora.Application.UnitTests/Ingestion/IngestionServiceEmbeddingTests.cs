using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Ingestion.EmbedDocumentChunks;
using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;

namespace Lexora.Application.UnitTests.Ingestion;

public sealed class IngestionServiceEmbeddingTests
{
    [Fact]
    public async Task EmbedNextBatchAsync_MoreChunksThanTheBatch_ReturnsTheNextBatchMessage()
    {
        var harness = new IngestionHarness(embeddingBatchSize: 2);
        var document = await harness.AddChunkedAsync(chunkCount: 5);
        var message = new EmbedPendingChunks(document.Id, 1);

        var next = await harness.Service.EmbedNextBatchAsync(message, TestContext.Current.CancellationToken);

        next.ShouldBe(message);
        document.EmbeddedChunkCount.ShouldBe(2);
        document.Status.ShouldBe(DocumentStatus.Chunked);
        harness.Chunks.Chunks.Count(chunk => chunk.HasEmbedding).ShouldBe(2);
    }

    [Fact]
    public async Task EmbedNextBatchAsync_LastBatch_CompletesIndexingWithTheEmbeddingSpace()
    {
        var harness = new IngestionHarness(embeddingBatchSize: 2);
        var document = await harness.AddChunkedAsync(chunkCount: 3);
        var message = new EmbedPendingChunks(document.Id, 1);

        (await harness.Service.EmbedNextBatchAsync(message, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        var last = await harness.Service.EmbedNextBatchAsync(message, TestContext.Current.CancellationToken);

        last.ShouldBeNull();
        document.Status.ShouldBe(DocumentStatus.Indexed);
        document.EmbeddingModel.ShouldBe(harness.Embeddings.Space.Key);
        document.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DocumentIndexed>();
        harness.Chunks.Chunks.ShouldAllBe(chunk => chunk.HasEmbedding && chunk.EmbeddingModel == harness.Embeddings.Space.Key);
    }

    [Fact]
    public async Task EmbedNextBatchAsync_Inputs_CarryTheDocumentAndSectionAsContext()
    {
        var harness = new IngestionHarness();
        var document = await harness.AddChunkedAsync(chunkCount: 2);

        await harness.Service.EmbedNextBatchAsync(new EmbedPendingChunks(document.Id, 1), TestContext.Current.CancellationToken);

        var call = harness.Embeddings.Calls.ShouldHaveSingleItem();
        call.Purpose.ShouldBe(EmbeddingPurpose.Document);
        call.Inputs.ShouldBe([$"{document.FileName.Value} > Safety\nchunk 0", $"{document.FileName.Value}\nchunk 1"]);
        harness.Chunks.Chunks.Select(chunk => chunk.Content).ShouldBe(["chunk 0", "chunk 1"]);
    }

    [Fact]
    public async Task EmbedNextBatchAsync_ProviderUnavailable_LeavesTheBatchForTheRetry()
    {
        var harness = new IngestionHarness();
        var document = await harness.AddChunkedAsync(chunkCount: 2);
        harness.Embeddings.FailWith = new AiProviderUnavailableException("ollama", "local", retryAfter: null, new HttpRequestException("503"));

        await Should.ThrowAsync<AiProviderUnavailableException>(() =>
            harness.Service.EmbedNextBatchAsync(new EmbedPendingChunks(document.Id, 1), TestContext.Current.CancellationToken));

        document.EmbeddedChunkCount.ShouldBe(0);
        harness.Chunks.Chunks.ShouldAllBe(chunk => !chunk.HasEmbedding);
    }

    [Fact]
    public async Task EmbedNextBatchAsync_MessageOfAnOlderVersion_IsDropped()
    {
        var harness = new IngestionHarness();
        var document = await harness.AddChunkedAsync(chunkCount: 2);

        var next = await harness.Service.EmbedNextBatchAsync(new EmbedPendingChunks(document.Id, 0), TestContext.Current.CancellationToken);

        next.ShouldBeNull();
        harness.Embeddings.Calls.ShouldBeEmpty();
    }

    [Fact]
    public void DocumentChunkedHandler_Event_StartsTheFirstBatch()
    {
        var documentId = Guid.CreateVersion7();

        DocumentChunkedHandler.Handle(new DocumentChunked(Guid.CreateVersion7(), documentId, 3, DateTimeOffset.UtcNow))
            .ShouldBe(new EmbedPendingChunks(documentId, 3));
    }
}
