using Lexora.Application.Common.Options;
using Lexora.Application.Documents;
using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;
using Lexora.Domain.RefreshTokens;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Application.UnitTests.Ingestion;

public sealed class MaintenanceServiceTests
{
    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly InMemoryDocumentRepository _documents = new();
    private readonly InMemoryDocumentChunkRepository _chunks = new();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();
    private readonly InMemoryFileStorage _storage;
    private readonly MaintenanceService _service;

    public MaintenanceServiceTests()
    {
        _storage = new InMemoryFileStorage(_time);
        _service = new MaintenanceService(
            _documents,
            _chunks,
            _refreshTokens,
            _storage,
            Options.Create(new IngestionOptions { StalledAfter = TimeSpan.FromMinutes(30) }),
            _time,
            NullLogger<MaintenanceService>.Instance);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RemoveDocumentDataAsync_DeletedDocument_RemovesItsFilesAndChunksOnly()
    {
        var removed = new DocumentBuilder().Build();
        var kept = new DocumentBuilder().InKnowledgeBase(removed.KnowledgeBaseId).Build();
        await StoreAsync(DocumentStorageKeys.OriginalOf(removed.KnowledgeBaseId, removed.Id, DocumentFormat.Pdf));
        await StoreAsync(DocumentStorageKeys.ExtractedOf(removed.KnowledgeBaseId, removed.Id, 1));
        await StoreAsync(DocumentStorageKeys.OriginalOf(kept.KnowledgeBaseId, kept.Id, DocumentFormat.Pdf));
        await _chunks.AddRangeAsync([Chunk(removed), Chunk(kept)], CancellationToken);

        await _service.RemoveDocumentDataAsync(removed.KnowledgeBaseId, removed.Id, CancellationToken);

        _storage.Keys.ShouldBe([DocumentStorageKeys.OriginalOf(kept.KnowledgeBaseId, kept.Id, DocumentFormat.Pdf)]);
        _chunks.Chunks.ShouldHaveSingleItem().DocumentId.ShouldBe(kept.Id);
    }

    [Fact]
    public async Task SweepStalledDocumentsAsync_DocumentsStuckInAStage_AreFailedWithTheTimeoutReason()
    {
        var stalled = await AddDocumentAsync(DocumentStatus.Extracted, updatedAt: TestClock.Now.AddHours(-1));
        var recent = await AddDocumentAsync(DocumentStatus.Chunked, updatedAt: TestClock.Now.AddMinutes(-10));
        var indexed = await AddDocumentAsync(DocumentStatus.Indexed, updatedAt: TestClock.Now.AddDays(-2));

        var count = await _service.SweepStalledDocumentsAsync(CancellationToken);

        count.ShouldBe(1);
        stalled.Status.ShouldBe(DocumentStatus.Failed);
        stalled.FailureReason.ShouldBe(MaintenanceService.StalledReason);
        stalled.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DocumentFailed>();
        recent.Status.ShouldBe(DocumentStatus.Chunked);
        indexed.Status.ShouldBe(DocumentStatus.Indexed);
    }

    [Fact]
    public async Task SweepOrphanFilesAsync_OldFilesWithoutAnActiveDocument_AreRemoved()
    {
        var active = await AddDocumentAsync(DocumentStatus.Indexed, updatedAt: TestClock.Now);
        var deleted = await AddDocumentAsync(DocumentStatus.Indexed, updatedAt: TestClock.Now);
        deleted.Delete(TestClock.Now);
        var missingId = Guid.CreateVersion7();
        var activeKey = DocumentStorageKeys.OriginalOf(active.KnowledgeBaseId, active.Id, DocumentFormat.Pdf);
        var deletedKey = DocumentStorageKeys.OriginalOf(deleted.KnowledgeBaseId, deleted.Id, DocumentFormat.Pdf);
        var missingKey = DocumentStorageKeys.OriginalOf(active.KnowledgeBaseId, missingId, DocumentFormat.Pdf);
        var freshKey = DocumentStorageKeys.OriginalOf(active.KnowledgeBaseId, Guid.CreateVersion7(), DocumentFormat.Pdf);
        foreach (var key in new[] { activeKey, deletedKey, missingKey })
        {
            await StoreAsync(key, modified: TestClock.Now.AddDays(-2));
        }

        await StoreAsync(freshKey, modified: TestClock.Now.AddHours(-1));
        await StoreAsync("knowledge-bases/not-a-guid/documents/x/original.pdf", modified: TestClock.Now.AddDays(-2));

        var removed = await _service.SweepOrphanFilesAsync(CancellationToken);

        removed.ShouldBe(2);
        _storage.Keys.ShouldBe([activeKey, freshKey, "knowledge-bases/not-a-guid/documents/x/original.pdf"], ignoreOrder: true);
    }

    [Fact]
    public async Task SweepExpiredRefreshTokensAsync_TokensFinishedLongAgo_AreDeleted()
    {
        var user = Guid.CreateVersion7();
        var expiredLongAgo = RefreshToken.Issue(user, "a", TestClock.Now.AddDays(-31), TestClock.Now.AddDays(-38));
        var revokedLongAgo = RefreshToken.Issue(user, "b", TestClock.Now.AddDays(10), TestClock.Now.AddDays(-40));
        revokedLongAgo.Revoke(RefreshTokenRevocationReason.Logout, TestClock.Now.AddDays(-35));
        var revokedRecently = RefreshToken.Issue(user, "c", TestClock.Now.AddDays(5), TestClock.Now.AddDays(-2));
        revokedRecently.Revoke(RefreshTokenRevocationReason.Logout, TestClock.Now.AddDays(-1));
        var active = RefreshToken.Issue(user, "d", TestClock.Now.AddDays(7), TestClock.Now);
        foreach (var token in new[] { expiredLongAgo, revokedLongAgo, revokedRecently, active })
        {
            await _refreshTokens.AddAsync(token, CancellationToken);
        }

        var deleted = await _service.SweepExpiredRefreshTokensAsync(CancellationToken);

        deleted.ShouldBe(2);
        _refreshTokens.Tokens.Select(token => token.TokenHash).ShouldBe(["c", "d"], ignoreOrder: true);
    }

    private async Task<Document> AddDocumentAsync(DocumentStatus status, DateTimeOffset updatedAt)
    {
        var document = new DocumentBuilder().InStatus(status).UploadedAt(updatedAt).Build();
        document.ClearDomainEvents();
        await _documents.AddAsync(document, CancellationToken);

        return document;
    }

    private async Task StoreAsync(string key, DateTimeOffset? modified = null)
    {
        using var content = new MemoryStream([1]);
        await _storage.SaveAsync(key, content, "application/octet-stream", CancellationToken);

        if (modified is { } value)
        {
            _storage.SetLastModified(key, value);
        }
    }

    private static DocumentChunk Chunk(Document document) =>
        DocumentChunk.Create(document.KnowledgeBaseId, document.Id, 1, 0, "content", 1, ChunkLocation.Create(1, null, null, 0, 7));
}
