using Lexora.Application.Documents;
using Lexora.Application.Documents.Abstractions;
using Lexora.Application.Documents.GetDocumentDownload;
using Lexora.TestKit;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Lexora.Application.UnitTests.Documents;

public sealed class GetDocumentDownloadHandlerTests
{
    private readonly IDocumentQueries _queries = Substitute.For<IDocumentQueries>();
    private readonly InMemoryFileStorage _storage = new(new FakeTimeProvider(TestClock.Now));

    [Fact]
    public async Task Handle_StoredDocument_ReturnsAShortLivedUrlForTheOriginal()
    {
        var id = Guid.CreateVersion7();
        _queries.GetStoredFileAsync(id, Arg.Any<CancellationToken>())
            .Returns(new StoredDocumentFile("knowledge-bases/kb/documents/doc/original.pdf", "manual.pdf", "application/pdf"));

        var result = await GetDocumentDownloadHandler.Handle(new GetDocumentDownloadQuery(id), _queries, _storage, TestContext.Current.CancellationToken);

        result.Value.Url.ShouldBe(new Uri("https://storage.test/knowledge-bases/kb/documents/doc/original.pdf?name=manual.pdf"));
        result.Value.ExpiresAt.ShouldBe(TestClock.Now + InMemoryFileStorage.DownloadLifetime);
    }

    [Fact]
    public async Task Handle_UnknownOrDeletedDocument_FailsAsNotFound() =>
        (await GetDocumentDownloadHandler.Handle(new GetDocumentDownloadQuery(Guid.CreateVersion7()), _queries, _storage, TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([DocumentNotifications.NotFound]);
}
