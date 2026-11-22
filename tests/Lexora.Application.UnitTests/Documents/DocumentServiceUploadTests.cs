using System.Security.Cryptography;
using System.Text;
using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Common.Options;
using Lexora.Application.Documents;
using Lexora.Application.Documents.Services;
using Lexora.Application.Documents.UploadDocument;
using Lexora.Application.KnowledgeBases;
using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;
using Lexora.Domain.KnowledgeBases;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Lexora.Application.UnitTests.Documents;

public sealed class DocumentServiceUploadTests
{
    private static readonly byte[] _pdf = Encoding.ASCII.GetBytes("%PDF-1.7 sample content");

    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly InMemoryDocumentRepository _documents = new();
    private readonly InMemoryKnowledgeBaseRepository _knowledgeBases = new();
    private readonly IDocumentTextExtractor _extractor = Substitute.For<IDocumentTextExtractor>();
    private readonly InMemoryFileStorage _storage;
    private readonly FakeCurrentUser _currentUser = new FakeCurrentUser().SignIn(new UserBuilder().Build());
    private readonly KnowledgeBase _knowledgeBase = new KnowledgeBaseBuilder().WithName("Safety Procedures").Build();
    private DocumentOptions _options = new();

    public DocumentServiceUploadTests()
    {
        _storage = new InMemoryFileStorage(_time);
        _knowledgeBases.AddAsync(_knowledgeBase, CancellationToken.None);
        _extractor.DetectFormatAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(DocumentFormat.Pdf);
    }

    [Fact]
    public async Task UploadAsync_ValidPdf_StoresItAndRegistersTheUploadedDocument()
    {
        var result = await UploadAsync("manual.pdf", _pdf);

        var document = _documents.Documents.ShouldHaveSingleItem();
        result.Value.ShouldBe(new UploadDocumentResponse(document.Id, "manual.pdf", DocumentFormat.Pdf, DocumentStatus.Uploaded, _pdf.Length));
        document.StorageKey.ShouldBe($"knowledge-bases/{_knowledgeBase.Id}/documents/{document.Id}/original.pdf");
        document.ContentType.ShouldBe("application/pdf");
        document.ContentHash.Value.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(_pdf)));
        document.UploadedBy.ShouldBe(_currentUser.UserId);
        document.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DocumentUploaded>().DocumentId.ShouldBe(document.Id);
        _storage.ContentOf(document.StorageKey).ShouldBe(_pdf);
        _storage.ContentTypeOf(document.StorageKey).ShouldBe("application/pdf");
    }

    [Fact]
    public async Task UploadAsync_StreamThatCannotSeek_IsBufferedAndHashedFromTheStart()
    {
        await using var content = new ForwardOnlyStream(_pdf);

        var result = await NewService().UploadAsync(
            new UploadDocumentCommand(_knowledgeBase.Id, "manual.pdf", "application/pdf", _pdf.Length, content),
            TestContext.Current.CancellationToken);

        result.Value.SizeBytes.ShouldBe(_pdf.Length);
        _documents.Documents.Single().ContentHash.Value.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(_pdf)));
    }

    [Fact]
    public async Task UploadAsync_UnknownKnowledgeBase_FailsAsNotFound()
    {
        var result = await NewService().UploadAsync(
            new UploadDocumentCommand(Guid.CreateVersion7(), "manual.pdf", null, _pdf.Length, new MemoryStream(_pdf)),
            TestContext.Current.CancellationToken);

        result.Notifications.ShouldBe([KnowledgeBaseNotifications.NotFound]);
    }

    [Fact]
    public async Task UploadAsync_SignatureNotRecognized_FailsAsUnsupportedWithoutStoring()
    {
        _extractor.DetectFormatAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DocumentFormat?)null);

        var result = await UploadAsync("manual.pdf", Encoding.UTF8.GetBytes("plain text"));

        result.Notifications.Single().Key.ShouldBe("document.format.unsupported");
        _storage.Keys.ShouldBeEmpty();
        _documents.Documents.ShouldBeEmpty();
    }

    [Fact]
    public async Task UploadAsync_SameContentTwice_FailsAsDuplicateWithoutStoringAgain()
    {
        await UploadAsync("manual.pdf", _pdf);

        var result = await UploadAsync("copy-of-manual.pdf", _pdf);

        result.Notifications.ShouldBe([DocumentNotifications.Duplicate]);
        _storage.Keys.Count.ShouldBe(1);
    }

    [Fact]
    public async Task UploadAsync_SameContentInAnotherBase_IsAccepted()
    {
        await UploadAsync("manual.pdf", _pdf);
        var other = new KnowledgeBaseBuilder().WithName("Quality Manuals").Build();
        await _knowledgeBases.AddAsync(other, TestContext.Current.CancellationToken);

        var result = await NewService().UploadAsync(
            new UploadDocumentCommand(other.Id, "manual.pdf", null, _pdf.Length, new MemoryStream(_pdf)),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UploadAsync_ArchivedKnowledgeBase_IsRejected()
    {
        _knowledgeBase.Archive(TestClock.Now);

        (await UploadAsync("manual.pdf", _pdf)).Notifications.Single().Key.ShouldBe("knowledgeBase.archived");
    }

    [Fact]
    public async Task UploadAsync_FileAboveTheSizeLimit_IsRejected()
    {
        _options = new DocumentOptions { MaxFileSizeBytes = 10, MaxTotalStorageBytes = 1_000 };

        (await UploadAsync("manual.pdf", _pdf)).Notifications.Single().Key.ShouldBe("document.size.exceeded");
    }

    [Fact]
    public async Task UploadAsync_StorageAlmostFull_IsRejected()
    {
        _options = new DocumentOptions { MaxFileSizeBytes = 30, MaxTotalStorageBytes = 30 };
        await _documents.AddAsync(new DocumentBuilder().WithSize(20).Build(), TestContext.Current.CancellationToken);

        (await UploadAsync("manual.pdf", _pdf)).Notifications.Single().Key.ShouldBe("storage.limit.exceeded");
    }

    [Fact]
    public async Task UploadAsync_FileNameWithAPath_KeepsOnlyTheName()
    {
        await UploadAsync("../../secret/manual.pdf", _pdf);

        _documents.Documents.Single().FileName.Value.ShouldBe("manual.pdf");
    }

    [Fact]
    public async Task UploadAsync_FileNameWithoutCharactersLeft_FailsAsInvalid() =>
        (await UploadAsync("...", _pdf)).Notifications.Single().Key.ShouldBe("document.fileName.invalid");

    private DocumentService NewService() =>
        new(_documents, _knowledgeBases, _extractor, _storage, _currentUser, Options.Create(_options), _time);

    private Task<Result<UploadDocumentResponse>> UploadAsync(string fileName, byte[] content) =>
        NewService().UploadAsync(
            new UploadDocumentCommand(_knowledgeBase.Id, fileName, "application/pdf", content.Length, new MemoryStream(content)),
            TestContext.Current.CancellationToken);
}
