using System.Globalization;
using System.Net;
using System.Text;
using Lexora.Infrastructure.Storage;
using Lexora.Infrastructure.Storage.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Infrastructure.IntegrationTests.Storage;

public sealed class S3FileStorageTests(SeaweedFsFixture seaweedFs) : IClassFixture<SeaweedFsFixture>
{
    private readonly FakeTimeProvider _time = new(DateTimeOffset.UtcNow);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private string Prefix { get; } = $"knowledge-bases/{Guid.CreateVersion7()}/documents/{Guid.CreateVersion7()}/";

    [Fact]
    public async Task SaveAsync_ThenOpenReadAsync_RoundTripsTheBytes()
    {
        var storage = seaweedFs.CreateStorage(_time);

        var stored = await SaveAsync(storage, Prefix + "original.txt", "hello seaweed");
        await using var read = await storage.OpenReadAsync(stored, CancellationToken);
        using var reader = new StreamReader(read);

        (await reader.ReadToEndAsync(CancellationToken)).ShouldBe("hello seaweed");
    }

    [Fact]
    public async Task SaveAsync_StreamAlreadyAdvanced_StoresFromTheCurrentPosition()
    {
        var storage = seaweedFs.CreateStorage(_time);
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("skip:kept"));
        content.Position = 5;

        var stored = await storage.SaveAsync(Prefix + "partial.txt", content, "text/plain", CancellationToken);

        stored.SizeBytes.ShouldBe(4);
    }

    [Fact]
    public async Task OpenReadAsync_MissingKey_ThrowsFileNotFound() =>
        await Should.ThrowAsync<FileNotFoundException>(() => seaweedFs.CreateStorage(_time).OpenReadAsync(Prefix + "missing.pdf", CancellationToken));

    [Fact]
    public async Task DeleteByPrefixAsync_DocumentPrefix_RemovesOnlyThatDocument()
    {
        var storage = seaweedFs.CreateStorage(_time);
        await SaveAsync(storage, Prefix + "original.pdf", "original");
        await SaveAsync(storage, Prefix + "extracted-v1.json", "extracted");
        var other = $"knowledge-bases/{Guid.CreateVersion7()}/documents/{Guid.CreateVersion7()}/original.pdf";
        await SaveAsync(storage, other, "other");

        await storage.DeleteByPrefixAsync(Prefix, CancellationToken);

        await Should.ThrowAsync<FileNotFoundException>(() => storage.OpenReadAsync(Prefix + "original.pdf", CancellationToken));
        await Should.ThrowAsync<FileNotFoundException>(() => storage.OpenReadAsync(Prefix + "extracted-v1.json", CancellationToken));
        (await storage.OpenReadAsync(other, CancellationToken)).Dispose();
    }

    [Fact]
    public async Task CreateDownloadAsync_StoredFile_ReturnsAPresignedAttachmentUrl()
    {
        var storage = seaweedFs.CreateStorage(_time);
        var key = await SaveAsync(storage, Prefix + "original.pdf", "%PDF-1.7 content");

        var download = await storage.CreateDownloadAsync(key, "relatório final.pdf", "application/pdf", CancellationToken);
        using var http = new HttpClient();
        using var response = await http.GetAsync(download.Url, CancellationToken);

        download.ExpiresAt.ShouldBe(_time.GetUtcNow() + new StorageOptions().DownloadUrlLifetime);
        ExpiresSecondsOf(download.Url).ShouldBeInRange(290, 310);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        response.Content.Headers.ContentDisposition.FileNameStar.ShouldBe("relatório final.pdf");
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldBe("%PDF-1.7 content");
    }

    [Fact]
    public async Task CreateDownloadAsync_UrlWithATamperedKey_IsRefused()
    {
        var storage = seaweedFs.CreateStorage(_time);
        var key = await SaveAsync(storage, Prefix + "original.pdf", "secret");
        var download = await storage.CreateDownloadAsync(key, "original.pdf", "application/pdf", CancellationToken);
        var tampered = new Uri(download.Url.ToString().Replace("original.pdf?", "other.pdf?", StringComparison.Ordinal));

        using var http = new HttpClient();
        using var response = await http.GetAsync(tampered, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HealthCheck_ExistingBucket_IsHealthy() =>
        (await CheckAsync(seaweedFs.Options)).Status.ShouldBe(HealthStatus.Healthy);

    [Fact]
    public async Task HealthCheck_MissingBucket_IsUnhealthy() =>
        (await CheckAsync(seaweedFs.For("missing-bucket"))).Status.ShouldBe(HealthStatus.Unhealthy);

    private static async Task<string> SaveAsync(S3FileStorage storage, string key, string text)
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes(text));

        return (await storage.SaveAsync(key, content, "text/plain", CancellationToken)).Key;
    }

    private static int ExpiresSecondsOf(Uri url) =>
        int.Parse(
            url.Query.TrimStart('?').Split('&').Single(pair => pair.StartsWith("X-Amz-Expires=", StringComparison.Ordinal))["X-Amz-Expires=".Length..],
            CultureInfo.InvariantCulture);

    private static async Task<HealthCheckResult> CheckAsync(S3StorageOptions options)
    {
        using var clients = new S3ClientFactory(Options.Create(options));
        var check = new S3StorageHealthCheck(clients, Options.Create(options));
        var registration = new HealthCheckRegistration("storage", check, HealthStatus.Unhealthy, tags: null);

        return await check.CheckHealthAsync(new HealthCheckContext { Registration = registration }, CancellationToken);
    }
}
