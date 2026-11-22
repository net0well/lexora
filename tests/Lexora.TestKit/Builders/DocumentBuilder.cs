using System.Security.Cryptography;
using Lexora.Domain.Documents;

namespace Lexora.TestKit.Builders;

public sealed class DocumentBuilder
{
    public const string DefaultEmbeddingModel = "local/bge-m3/1024";
    public const string DefaultFailureReason = "The file could not be read.";
    public const int DefaultPageCount = 3;

    private Guid _knowledgeBaseId = Guid.CreateVersion7();
    private Guid _uploadedBy = Guid.CreateVersion7();
    private ContentHash? _contentHash;
    private string _fileName = "contract.pdf";
    private DocumentFormat _format = DocumentFormat.Pdf;
    private long _sizeBytes = 2048;
    private int _chunkCount = 4;
    private DocumentStatus _status = DocumentStatus.Uploaded;
    private DateTimeOffset _now = TestClock.Now;

    public DocumentBuilder InKnowledgeBase(Guid knowledgeBaseId)
    {
        _knowledgeBaseId = knowledgeBaseId;
        return this;
    }

    public DocumentBuilder WithFileName(string fileName, DocumentFormat format)
    {
        _fileName = fileName;
        _format = format;
        return this;
    }

    public DocumentBuilder UploadedBy(Guid userId)
    {
        _uploadedBy = userId;
        return this;
    }

    public DocumentBuilder WithContentHash(ContentHash contentHash)
    {
        _contentHash = contentHash;
        return this;
    }

    public DocumentBuilder WithSize(long sizeBytes)
    {
        _sizeBytes = sizeBytes;
        return this;
    }

    public DocumentBuilder WithChunkCount(int chunkCount)
    {
        _chunkCount = chunkCount;
        return this;
    }

    public DocumentBuilder InStatus(DocumentStatus status)
    {
        _status = status;
        return this;
    }

    public DocumentBuilder UploadedAt(DateTimeOffset now)
    {
        _now = now;
        return this;
    }

    public Document Build()
    {
        var id = Guid.CreateVersion7();
        var document = Document.Upload(
            id,
            _knowledgeBaseId,
            _fileName,
            _format,
            "application/pdf",
            _sizeBytes,
            _contentHash ?? ContentHash.FromHex(Convert.ToHexStringLower(SHA256.HashData(id.ToByteArray()))),
            $"knowledge-bases/{_knowledgeBaseId}/documents/{id}/original",
            _uploadedBy,
            _now).Value;

        MoveTo(document, _status);
        document.ClearDomainEvents();

        return document;
    }

    private void MoveTo(Document document, DocumentStatus status)
    {
        var version = document.ProcessingVersion;

        switch (status)
        {
            case DocumentStatus.Uploaded:
                return;
            case DocumentStatus.Failed:
                document.Fail(version, DefaultFailureReason, _now);
                return;
            case DocumentStatus.Deleted:
                document.Delete(_now);
                return;
        }

        document.CompleteExtraction(version, DefaultPageCount, $"documents/{document.Id}/v{version}/extracted.json", _now);

        if (status == DocumentStatus.Extracted)
        {
            return;
        }

        document.CompleteChunking(version, _chunkCount, _now);

        if (status == DocumentStatus.Chunked)
        {
            return;
        }

        document.RegisterEmbeddedBatch(version, _chunkCount, _now);
        document.CompleteIndexing(version, DefaultEmbeddingModel, _now);
    }
}
