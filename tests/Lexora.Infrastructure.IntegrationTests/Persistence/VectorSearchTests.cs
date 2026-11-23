using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.Persistence;
using Lexora.Infrastructure.Persistence.Search;
using Lexora.TestKit;
using Lexora.TestKit.Builders;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class VectorSearchTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string Space = "fake:fake-embedding:1024";

    [Fact]
    public async Task VectorSearchAsync_IndexedChunks_ComeNearestFirstWithTheirCosineSimilarity()
    {
        var (knowledgeBaseId, document) = await SaveIndexedDocumentAsync("manual.pdf");
        await SaveAsync(
            Chunk(document, 0, "far", Direction(1)),
            Chunk(document, 1, "near", Direction(0)),
            Chunk(document, 2, "between", Direction(0, 1)));

        var hits = await SearchAsync(knowledgeBaseId, Direction(0));

        hits.Select(hit => hit.Content).ShouldBe(["near", "between", "far"]);
        hits[0].Score.ShouldBe(1, tolerance: 0.0001);
        hits[1].Score.ShouldBe(Math.Sqrt(0.5), tolerance: 0.0001);
        hits[0].ShouldSatisfyAllConditions(
            hit => hit.DocumentId.ShouldBe(document.Id),
            hit => hit.DocumentName.ShouldBe("manual.pdf"),
            hit => hit.PageNumber.ShouldBe(3),
            hit => hit.SectionTitle.ShouldBe("Safety"));
    }

    [Fact]
    public async Task VectorSearchAsync_ChunksThatAreNotCurrentlySearchable_AreLeftOut()
    {
        var (knowledgeBaseId, indexed) = await SaveIndexedDocumentAsync("indexed.pdf");
        var (otherBaseId, otherBase) = await SaveIndexedDocumentAsync("other-base.pdf");
        var stillEmbedding = await SaveDocumentAsync(knowledgeBaseId, DocumentStatus.Chunked);
        var deleted = await SaveDocumentAsync(knowledgeBaseId, DocumentStatus.Indexed, document => document.Delete(TestClock.Now));
        var noVector = DocumentChunk.Create(indexed.KnowledgeBaseId, indexed.Id, 1, 9, "no vector", 2, Location());
        await SaveAsync(
            Chunk(indexed, 0, "visible", Direction(0)),
            Chunk(indexed, 1, "old version", Direction(0), processingVersion: 2),
            Chunk(indexed, 2, "other space", Direction(0), space: "ollama:bge-m3:1024"),
            noVector,
            Chunk(otherBase, 0, "other base", Direction(0)),
            Chunk(stillEmbedding, 0, "not indexed", Direction(0)),
            Chunk(deleted, 0, "deleted", Direction(0)));

        (await SearchAsync(knowledgeBaseId, Direction(0))).Select(hit => hit.Content).ShouldBe(["visible"]);
        (await SearchAsync(otherBaseId, Direction(0))).Select(hit => hit.Content).ShouldBe(["other base"]);
    }

    [Fact]
    public async Task VectorSearchAsync_Limit_CapsTheCandidates()
    {
        var (knowledgeBaseId, document) = await SaveIndexedDocumentAsync("manual.pdf");
        await SaveAsync([.. Enumerable.Range(0, 5).Select(index => Chunk(document, index, $"chunk {index}", Direction(0, index + 1)))]);

        (await SearchAsync(knowledgeBaseId, Direction(0), limit: 3)).Count.ShouldBe(3);
    }

    private async Task<IReadOnlyList<Application.Abstractions.Search.ChunkSearchCandidate>> SearchAsync(Guid knowledgeBaseId, float[] query, int limit = 10)
    {
        await using var context = Database.CreateContext();

        return await new ChunkSearchIndex(context).VectorSearchAsync(knowledgeBaseId, query, Space, limit, CancellationToken);
    }

    private async Task<(Guid KnowledgeBaseId, Document Document)> SaveIndexedDocumentAsync(string fileName)
    {
        var owner = await SaveUserAsync($"{Guid.CreateVersion7():N}@lexora.test");
        var knowledgeBase = new KnowledgeBaseBuilder().WithName($"Base {Guid.CreateVersion7():N}").CreatedBy(owner.Id).Build();
        var document = new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(owner.Id).WithFileName(fileName, DocumentFormat.Pdf).InStatus(DocumentStatus.Indexed).Build();
        await SaveAsync(knowledgeBase, document);

        return (knowledgeBase.Id, document);
    }

    private async Task<Document> SaveDocumentAsync(Guid knowledgeBaseId, DocumentStatus status, Action<Document>? change = null)
    {
        await using var context = Database.CreateContext();
        var uploadedBy = context.KnowledgeBases.Single(knowledgeBase => knowledgeBase.Id == knowledgeBaseId).CreatedBy;
        var document = new DocumentBuilder().InKnowledgeBase(knowledgeBaseId).UploadedBy(uploadedBy).InStatus(status).Build();
        change?.Invoke(document);
        await SaveAsync(document);

        return document;
    }

    private static DocumentChunk Chunk(Document document, int index, string content, float[] embedding, int processingVersion = 1, string space = Space)
    {
        var chunk = DocumentChunk.Create(document.KnowledgeBaseId, document.Id, processingVersion, index, content, 2, Location());
        chunk.AttachEmbedding(embedding, space, TestClock.Now);

        return chunk;
    }

    private static ChunkLocation Location() => ChunkLocation.Create(3, null, "Safety", 0, 10);

    private static float[] Direction(params int[] axes)
    {
        var vector = new float[SchemaHelper.EmbeddingDimensions];

        foreach (var axis in axes)
        {
            vector[axis] = 1;
        }

        return vector;
    }
}
