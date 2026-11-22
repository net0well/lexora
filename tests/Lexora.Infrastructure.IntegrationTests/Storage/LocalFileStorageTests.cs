using System.Text;
using Lexora.Infrastructure.Storage;
using Lexora.Infrastructure.Storage.Local;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Infrastructure.IntegrationTests.Storage;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lexora-local-storage-tests", Guid.CreateVersion7().ToString("N"));
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);
    private readonly LocalDownloadTickets _tickets = new(new EphemeralDataProtectionProvider());
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests() =>
        _storage = new LocalFileStorage(
            Options.Create(new LocalStorageOptions { RootPath = _root, DownloadBaseUrl = new Uri("https://lexora.test/api/v1/files/") }),
            Options.Create(new StorageOptions()),
            _tickets,
            _time);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveAsync_ThenOpenReadAsync_RoundTripsTheBytes()
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello storage"));

        var stored = await _storage.SaveAsync("knowledge-bases/kb/documents/doc/original.txt", content, "text/plain", CancellationToken);
        await using var read = await _storage.OpenReadAsync(stored.Key, CancellationToken);
        using var reader = new StreamReader(read);

        stored.SizeBytes.ShouldBe(13);
        (await reader.ReadToEndAsync(CancellationToken)).ShouldBe("hello storage");
    }

    [Fact]
    public async Task OpenReadAsync_MissingKey_ThrowsFileNotFound() =>
        await Should.ThrowAsync<FileNotFoundException>(() => _storage.OpenReadAsync("missing/file.pdf", CancellationToken));

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("knowledge-bases/../../outside.txt")]
    public async Task SaveAsync_KeyEscapingTheRoot_IsRejected(string key)
    {
        using var content = new MemoryStream([1]);

        await Should.ThrowAsync<ArgumentException>(() => _storage.SaveAsync(key, content, "application/octet-stream", CancellationToken));
    }

    [Fact]
    public async Task DeleteByPrefixAsync_DocumentPrefix_RemovesOnlyThatDocument()
    {
        await SaveAsync("knowledge-bases/kb/documents/one/original.pdf");
        await SaveAsync("knowledge-bases/kb/documents/one/extracted-v1.json");
        await SaveAsync("knowledge-bases/kb/documents/two/original.pdf");

        await _storage.DeleteByPrefixAsync("knowledge-bases/kb/documents/one/", CancellationToken);

        await Should.ThrowAsync<FileNotFoundException>(() => _storage.OpenReadAsync("knowledge-bases/kb/documents/one/original.pdf", CancellationToken));
        Directory.Exists(Path.Combine(_root, "knowledge-bases", "kb", "documents", "one")).ShouldBeFalse();
        (await _storage.OpenReadAsync("knowledge-bases/kb/documents/two/original.pdf", CancellationToken)).Dispose();
    }

    [Fact]
    public async Task DeleteByPrefixAsync_MissingPrefix_DoesNothing() =>
        await _storage.DeleteByPrefixAsync("knowledge-bases/none/", CancellationToken);

    [Fact]
    public async Task CreateDownloadAsync_StoredFile_ReturnsATicketUrlThatResolvesToTheFile()
    {
        var download = await _storage.CreateDownloadAsync("knowledge-bases/kb/documents/one/original.pdf", "manual.pdf", "application/pdf", CancellationToken);

        download.ExpiresAt.ShouldBe(_time.GetUtcNow() + new StorageOptions().DownloadUrlLifetime);
        download.Url.GetLeftPart(UriPartial.Authority).ShouldBe("https://lexora.test");
        download.Url.AbsolutePath.ShouldStartWith("/api/v1/files/");
        _tickets.TryRead(Uri.UnescapeDataString(download.Url.Segments[^1]), out var ticket).ShouldBeTrue();
        ticket.ShouldBe(new LocalDownloadTicket("knowledge-bases/kb/documents/one/original.pdf", "manual.pdf", "application/pdf"));
    }

    [Fact]
    public void TryRead_ExpiredTicket_IsRejected()
    {
        var ticket = _tickets.Issue(new LocalDownloadTicket("key", "file.pdf", "application/pdf"), DateTimeOffset.UtcNow.AddMinutes(-1));

        _tickets.TryRead(ticket, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-ticket")]
    public void TryRead_ForgedTicket_IsRejected(string ticket) => _tickets.TryRead(ticket, out _).ShouldBeFalse();

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private async Task SaveAsync(string key)
    {
        using var content = new MemoryStream([1, 2, 3]);
        await _storage.SaveAsync(key, content, "application/octet-stream", CancellationToken);
    }
}
