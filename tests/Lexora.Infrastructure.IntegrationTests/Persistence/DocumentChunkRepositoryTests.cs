using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.Persistence;
using Lexora.Infrastructure.Persistence.Repositories;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class DocumentChunkRepositoryTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string EmbeddingModel = DocumentBuilder.DefaultEmbeddingModel;

    [Fact]
    public async Task AddRangeAsync_ChunkWithEmbedding_RoundTripsVectorAndLocation()
    {
        var document = await SaveDocumentAsync();
        var chunk = NewChunk(document, 1, 0, ChunkLocation.Create(3, null, "Payment terms", 120, 480));
        var embedding = Embedding(0.25f);
        chunk.AttachEmbedding(embedding, EmbeddingModel, TestClock.Now);

        await using (var context = Database.CreateContext())
        {
            await new DocumentChunkRepository(context).AddRangeAsync([chunk], CancellationToken);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using var reader = Database.CreateContext();
        var loaded = await reader.DocumentChunks.SingleAsync(candidate => candidate.Id == chunk.Id, CancellationToken);

        loaded.Content.ShouldBe(chunk.Content);
        loaded.TokenCount.ShouldBe(chunk.TokenCount);
        loaded.Location.ShouldBe(ChunkLocation.Create(3, null, "Payment terms", 120, 480));
        loaded.Embedding.ShouldBe(embedding);
        loaded.EmbeddingModel.ShouldBe(EmbeddingModel);
        loaded.EmbeddedAt.ShouldBe(TestClock.Now);
    }

    [Fact]
    public async Task GetPendingEmbeddingAsync_MixedChunks_ReturnsUnembeddedChunksOfTheVersionInOrder()
    {
        var document = await SaveDocumentAsync();
        var chunks = Enumerable.Range(0, 5).Select(index => NewChunk(document, 1, index)).ToList();
        chunks[1].AttachEmbedding(Embedding(0.5f), EmbeddingModel, TestClock.Now);
        await SaveAsync([.. chunks, NewChunk(document, 2, 0)]);

        await using var context = Database.CreateContext();
        var pending = await new DocumentChunkRepository(context).GetPendingEmbeddingAsync(document.Id, 1, 3, CancellationToken);

        pending.Select(chunk => chunk.Index).ShouldBe([0, 2, 3]);
    }

    [Fact]
    public async Task GetPendingEmbeddingAsync_AllChunksEmbedded_ReturnsEmpty()
    {
        var document = await SaveDocumentAsync();
        var chunk = NewChunk(document, 1, 0);
        chunk.AttachEmbedding(Embedding(0.5f), EmbeddingModel, TestClock.Now);
        await SaveAsync(chunk);

        await using var context = Database.CreateContext();

        (await new DocumentChunkRepository(context).GetPendingEmbeddingAsync(document.Id, 1, 10, CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteOtherVersionsAsync_ChunksOfTwoVersions_RemovesOnlyTheOtherVersion()
    {
        var document = await SaveDocumentAsync();
        await SaveAsync(NewChunk(document, 1, 0), NewChunk(document, 1, 1), NewChunk(document, 2, 0), NewChunk(document, 2, 1), NewChunk(document, 2, 2));

        await using var context = Database.CreateContext();
        var deleted = await new DocumentChunkRepository(context).DeleteOtherVersionsAsync(document.Id, 2, CancellationToken);

        deleted.ShouldBe(2);
        (await context.DocumentChunks.Select(chunk => chunk.ProcessingVersion).ToListAsync(CancellationToken)).ShouldAllBe(version => version == 2);
    }

    private async Task<Document> SaveDocumentAsync()
    {
        var owner = await SaveUserAsync();
        var knowledgeBase = new KnowledgeBaseBuilder().CreatedBy(owner.Id).Build();
        var document = new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(owner.Id).InStatus(DocumentStatus.Chunked).Build();
        await SaveAsync(knowledgeBase, document);

        return document;
    }

    private static DocumentChunk NewChunk(Document document, int processingVersion, int index, ChunkLocation? location = null) =>
        DocumentChunk.Create(
            document.KnowledgeBaseId,
            document.Id,
            processingVersion,
            index,
            $"Chunk {index} of version {processingVersion}.",
            6,
            location ?? ChunkLocation.Create(1, null, null, index * 100, (index * 100) + 99));

    private static float[] Embedding(float seed) =>
        [.. Enumerable.Range(0, SchemaHelper.EmbeddingDimensions).Select(position => seed + (position / 10_000f))];
}
