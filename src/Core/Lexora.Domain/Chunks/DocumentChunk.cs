using Lexora.Shared.Domain;
using Lexora.Shared.Guards;

namespace Lexora.Domain.Chunks;

public sealed class DocumentChunk : AggregateRoot
{
    private DocumentChunk()
    {
    }

    private DocumentChunk(
        Guid knowledgeBaseId,
        Guid documentId,
        int processingVersion,
        int index,
        string content,
        int tokenCount,
        ChunkLocation location)
        : base(Guid.CreateVersion7())
    {
        KnowledgeBaseId = knowledgeBaseId;
        DocumentId = documentId;
        ProcessingVersion = processingVersion;
        Index = index;
        Content = content;
        TokenCount = tokenCount;
        Location = location;
    }

    public Guid KnowledgeBaseId { get; private set; }

    public Guid DocumentId { get; private set; }

    public int ProcessingVersion { get; private set; }

    public int Index { get; private set; }

    public string Content { get; private set; } = null!;

    public int TokenCount { get; private set; }

    public ChunkLocation Location { get; private set; } = null!;

    public float[]? Embedding { get; private set; }

    public string? EmbeddingModel { get; private set; }

    public DateTimeOffset? EmbeddedAt { get; private set; }

    public bool HasEmbedding => Embedding is not null;

    public static DocumentChunk Create(
        Guid knowledgeBaseId,
        Guid documentId,
        int processingVersion,
        int index,
        string content,
        int tokenCount,
        ChunkLocation location) =>
        new(
            Guard.NotEmpty(knowledgeBaseId),
            Guard.NotEmpty(documentId),
            Guard.Positive(processingVersion),
            Guard.NotNegative(index),
            Guard.NotNullOrWhiteSpace(content),
            Guard.Positive(tokenCount),
            Guard.NotNull(location));

    public void AttachEmbedding(ReadOnlySpan<float> embedding, string embeddingModel, DateTimeOffset now)
    {
        if (HasEmbedding)
        {
            throw new InvalidOperationException("The chunk already has an embedding.");
        }

        if (embedding.IsEmpty)
        {
            throw new ArgumentException("The embedding cannot be empty.", nameof(embedding));
        }

        EmbeddingModel = Guard.NotNullOrWhiteSpace(embeddingModel);
        Embedding = embedding.ToArray();
        EmbeddedAt = now;
    }
}
