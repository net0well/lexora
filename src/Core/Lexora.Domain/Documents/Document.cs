using Lexora.Shared.Domain;
using Lexora.Shared.Guards;
using Lexora.Shared.Results;

namespace Lexora.Domain.Documents;

public sealed class Document : AggregateRoot
{
    public const int InitialProcessingVersion = 1;

    private static readonly Notification _stale =
        Notification.BusinessRule("document.processing.stale", "The document was reprocessed; this step belongs to an older version.");

    private static readonly Notification _invalidTransition =
        Notification.BusinessRule("document.status.invalidTransition", "The document cannot move to the requested status.");

    private Document()
    {
    }

    private Document(
        Guid id,
        Guid knowledgeBaseId,
        FileName fileName,
        DocumentFormat format,
        string contentType,
        long sizeBytes,
        ContentHash contentHash,
        string storageKey,
        Guid uploadedBy,
        DateTimeOffset now)
        : base(id)
    {
        KnowledgeBaseId = knowledgeBaseId;
        FileName = fileName;
        Format = format;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        ContentHash = contentHash;
        StorageKey = storageKey;
        UploadedBy = uploadedBy;
        Status = DocumentStatus.Uploaded;
        ProcessingVersion = InitialProcessingVersion;
        UploadedAt = now;
        UpdatedAt = now;
    }

    public Guid KnowledgeBaseId { get; private set; }

    public FileName FileName { get; private set; } = null!;

    public DocumentFormat Format { get; private set; }

    public string ContentType { get; private set; } = null!;

    public long SizeBytes { get; private set; }

    public ContentHash ContentHash { get; private set; } = null!;

    public string StorageKey { get; private set; } = null!;

    public string? ExtractedTextKey { get; private set; }

    public DocumentStatus Status { get; private set; }

    public int ProcessingVersion { get; private set; }

    public int? PageCount { get; private set; }

    public int ChunkCount { get; private set; }

    public int EmbeddedChunkCount { get; private set; }

    public string? EmbeddingModel { get; private set; }

    public string? FailureReason { get; private set; }

    public Guid UploadedBy { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public DateTimeOffset? IndexedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool HasPendingEmbeddings => EmbeddedChunkCount < ChunkCount;

    public static Result<Document> Upload(
        Guid id,
        Guid knowledgeBaseId,
        string? fileName,
        DocumentFormat format,
        string contentType,
        long sizeBytes,
        ContentHash contentHash,
        string storageKey,
        Guid uploadedBy,
        DateTimeOffset now)
    {
        Guard.NotEmpty(id);
        Guard.NotEmpty(knowledgeBaseId);
        Guard.Defined(format);
        Guard.NotNullOrWhiteSpace(contentType);
        Guard.Positive(sizeBytes);
        Guard.NotNull(contentHash);
        Guard.NotNullOrWhiteSpace(storageKey);
        Guard.NotEmpty(uploadedBy);

        var fileNameResult = FileName.Create(fileName);

        if (fileNameResult.IsFailure)
        {
            return Result.Failure<Document>(fileNameResult.Notifications);
        }

        return new Document(id, knowledgeBaseId, fileNameResult.Value, format, contentType, sizeBytes, contentHash, storageKey, uploadedBy, now);
    }

    public Result CompleteExtraction(int processingVersion, int pageCount, string extractedTextKey, DateTimeOffset now)
    {
        var allowed = EnsureCanMove(processingVersion, Status == DocumentStatus.Uploaded);

        if (allowed.IsFailure)
        {
            return allowed;
        }

        PageCount = Guard.Positive(pageCount);
        ExtractedTextKey = Guard.NotNullOrWhiteSpace(extractedTextKey);
        MoveTo(DocumentStatus.Extracted, now);

        return Result.Success();
    }

    public Result CompleteChunking(int processingVersion, int chunkCount, DateTimeOffset now)
    {
        var allowed = EnsureCanMove(processingVersion, Status == DocumentStatus.Extracted);

        if (allowed.IsFailure)
        {
            return allowed;
        }

        ChunkCount = Guard.Positive(chunkCount);
        EmbeddedChunkCount = 0;
        MoveTo(DocumentStatus.Chunked, now);

        return Result.Success();
    }

    public Result RegisterEmbeddedBatch(int processingVersion, int embeddedCount, DateTimeOffset now)
    {
        var allowed = EnsureCanMove(processingVersion, Status == DocumentStatus.Chunked);

        if (allowed.IsFailure)
        {
            return allowed;
        }

        Guard.Positive(embeddedCount);

        if (EmbeddedChunkCount + embeddedCount > ChunkCount)
        {
            throw new ArgumentOutOfRangeException(nameof(embeddedCount), embeddedCount, "More chunks embedded than the document has.");
        }

        EmbeddedChunkCount += embeddedCount;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result CompleteIndexing(int processingVersion, string embeddingModel, DateTimeOffset now)
    {
        var allowed = EnsureCanMove(processingVersion, Status == DocumentStatus.Chunked && !HasPendingEmbeddings);

        if (allowed.IsFailure)
        {
            return allowed;
        }

        EmbeddingModel = Guard.NotNullOrWhiteSpace(embeddingModel);
        IndexedAt = now;
        MoveTo(DocumentStatus.Indexed, now);

        return Result.Success();
    }

    public Result Fail(int processingVersion, string reason, DateTimeOffset now)
    {
        var allowed = EnsureCanMove(
            processingVersion,
            Status is DocumentStatus.Uploaded or DocumentStatus.Extracted or DocumentStatus.Chunked);

        if (allowed.IsFailure)
        {
            return allowed;
        }

        FailureReason = Guard.NotNullOrWhiteSpace(reason);
        MoveTo(DocumentStatus.Failed, now);

        return Result.Success();
    }

    public Result RequestReprocess(DateTimeOffset now)
    {
        if (Status is not (DocumentStatus.Indexed or DocumentStatus.Failed))
        {
            return Result.Failure(_invalidTransition);
        }

        ProcessingVersion++;
        ExtractedTextKey = null;
        PageCount = null;
        ChunkCount = 0;
        EmbeddedChunkCount = 0;
        EmbeddingModel = null;
        FailureReason = null;
        IndexedAt = null;
        MoveTo(DocumentStatus.Uploaded, now);

        return Result.Success();
    }

    public Result Delete(DateTimeOffset now)
    {
        if (Status == DocumentStatus.Deleted)
        {
            return Result.Failure(_invalidTransition);
        }

        DeletedAt = now;
        MoveTo(DocumentStatus.Deleted, now);

        return Result.Success();
    }

    private Result EnsureCanMove(int processingVersion, bool statusAllows)
    {
        if (processingVersion != ProcessingVersion)
        {
            return Result.Failure(_stale);
        }

        return statusAllows ? Result.Success() : Result.Failure(_invalidTransition);
    }

    private void MoveTo(DocumentStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }
}
