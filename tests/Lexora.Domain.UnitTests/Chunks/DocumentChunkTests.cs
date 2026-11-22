using Lexora.Domain.Chunks;
using Lexora.TestKit;

namespace Lexora.Domain.UnitTests.Chunks;

public sealed class DocumentChunkTests
{
    private const string EmbeddingModel = "local/bge-m3/1024";

    private static readonly Guid _knowledgeBaseId = Guid.CreateVersion7();
    private static readonly Guid _documentId = Guid.CreateVersion7();
    private static readonly ChunkLocation _location = ChunkLocation.Create(1, null, "Intro", 0, 120);

    [Fact]
    public void Create_ValidInput_StartsWithoutEmbedding()
    {
        var chunk = DocumentChunk.Create(_knowledgeBaseId, _documentId, 2, 0, "Payment terms are 30 days.", 7, _location);

        chunk.KnowledgeBaseId.ShouldBe(_knowledgeBaseId);
        chunk.DocumentId.ShouldBe(_documentId);
        chunk.ProcessingVersion.ShouldBe(2);
        chunk.Index.ShouldBe(0);
        chunk.Content.ShouldBe("Payment terms are 30 days.");
        chunk.TokenCount.ShouldBe(7);
        chunk.Location.ShouldBe(_location);
        chunk.HasEmbedding.ShouldBeFalse();
        chunk.Embedding.ShouldBeNull();
        chunk.EmbeddedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, 0, "text", 1)]
    [InlineData(1, -1, "text", 1)]
    [InlineData(1, 0, "   ", 1)]
    [InlineData(1, 0, "text", 0)]
    public void Create_InvalidArgument_Throws(int processingVersion, int index, string content, int tokenCount) =>
        Should.Throw<ArgumentException>(() =>
            DocumentChunk.Create(_knowledgeBaseId, _documentId, processingVersion, index, content, tokenCount, _location));

    [Fact]
    public void AttachEmbedding_FirstTime_StoresVectorModelAndTime()
    {
        var chunk = NewChunk();

        chunk.AttachEmbedding([0.1f, 0.2f, 0.3f], EmbeddingModel, TestClock.Now);

        chunk.HasEmbedding.ShouldBeTrue();
        chunk.Embedding.ShouldBe([0.1f, 0.2f, 0.3f]);
        chunk.EmbeddingModel.ShouldBe(EmbeddingModel);
        chunk.EmbeddedAt.ShouldBe(TestClock.Now);
    }

    [Fact]
    public void AttachEmbedding_SourceChangedAfterwards_KeepsOriginalVector()
    {
        var chunk = NewChunk();
        float[] source = [0.1f, 0.2f];

        chunk.AttachEmbedding(source, EmbeddingModel, TestClock.Now);
        source[0] = 9f;

        chunk.Embedding.ShouldBe([0.1f, 0.2f]);
    }

    [Fact]
    public void AttachEmbedding_AlreadyEmbedded_Throws()
    {
        var chunk = NewChunk();
        chunk.AttachEmbedding([0.1f], EmbeddingModel, TestClock.Now);

        Should.Throw<InvalidOperationException>(() => chunk.AttachEmbedding([0.2f], EmbeddingModel, TestClock.Now));
    }

    [Fact]
    public void AttachEmbedding_EmptyVector_Throws() =>
        Should.Throw<ArgumentException>(() => NewChunk().AttachEmbedding([], EmbeddingModel, TestClock.Now));

    [Fact]
    public void AttachEmbedding_BlankModel_Throws() =>
        Should.Throw<ArgumentException>(() => NewChunk().AttachEmbedding([0.1f], " ", TestClock.Now));

    private static DocumentChunk NewChunk() => DocumentChunk.Create(_knowledgeBaseId, _documentId, 1, 0, "Payment terms.", 3, _location);
}
