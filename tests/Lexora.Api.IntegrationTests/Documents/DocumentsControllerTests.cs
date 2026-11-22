using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Common.Paging;
using Lexora.Application.Documents.GetDocument;
using Lexora.Application.Documents.GetDocumentDownload;
using Lexora.Application.Documents.ListDocuments;
using Lexora.Application.Documents.UploadDocument;
using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;
using Lexora.Domain.Users;
using Lexora.TestKit.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Tracking;

namespace Lexora.Api.IntegrationTests.Documents;

public sealed class DocumentsControllerTests(ApiFactory api) : ApiTest(api)
{
    private static readonly byte[] _pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj << /Type /Catalog >> endobj\n%%EOF");

    [Fact]
    public async Task Upload_Pdf_RespondsAcceptedAndPublishesDocumentUploaded()
    {
        using var client = await MemberClientAsync();
        var knowledgeBaseId = await CreateKnowledgeBaseAsync();
        HttpResponseMessage? response = null;

        var session = await Api.Services.GetRequiredService<IHost>()
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ => response = await UploadAsync(client, knowledgeBaseId, "manual.pdf", _pdf)));

        using (response)
        {
            response!.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            var uploaded = (await response.ReadEnvelopeAsync<UploadDocumentResponse>(CancellationToken)).Data.ShouldNotBeNull();
            uploaded.Format.ShouldBe(DocumentFormat.Pdf);
            uploaded.Status.ShouldBe(DocumentStatus.Uploaded);
            uploaded.SizeBytes.ShouldBe(_pdf.Length);
            response.Headers.Location.ShouldBe(new Uri($"/api/v1/documents/{uploaded.Id}", UriKind.Relative));
            session.Sent.SingleMessage<DocumentUploaded>().DocumentId.ShouldBe(uploaded.Id);
            File.Exists(Path.Combine(Api.StorageRoot, "knowledge-bases", knowledgeBaseId.ToString(), "documents", uploaded.Id.ToString(), "original.pdf"))
                .ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Upload_SameFileTwice_RespondsConflict()
    {
        using var client = await MemberClientAsync();
        var knowledgeBaseId = await CreateKnowledgeBaseAsync();
        (await UploadAsync(client, knowledgeBaseId, "manual.pdf", _pdf)).Dispose();

        using var response = await UploadAsync(client, knowledgeBaseId, "renamed.pdf", _pdf);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("document.duplicate");
    }

    [Theory]
    [InlineData("manual.pdf", "plain text renamed to pdf")]
    [InlineData("report.docx", "%PDF-1.7 a pdf renamed to docx")]
    [InlineData("program.exe", "MZ")]
    public async Task Upload_ContentNotMatchingTheExtension_RespondsUnsupported(string fileName, string content)
    {
        using var client = await MemberClientAsync();
        var knowledgeBaseId = await CreateKnowledgeBaseAsync();

        using var response = await UploadAsync(client, knowledgeBaseId, fileName, Encoding.UTF8.GetBytes(content));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("document.format.unsupported");
    }

    [Fact]
    public async Task Upload_Docx_IsDetectedFromItsParts()
    {
        using var client = await MemberClientAsync();
        var knowledgeBaseId = await CreateKnowledgeBaseAsync();

        using var response = await UploadAsync(client, knowledgeBaseId, "report.docx", Docx());

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await response.ReadEnvelopeAsync<UploadDocumentResponse>(CancellationToken)).Data!.Format.ShouldBe(DocumentFormat.Docx);
    }

    [Fact]
    public async Task Upload_WithoutTheFileField_RespondsBadRequest()
    {
        using var client = await MemberClientAsync();
        var knowledgeBaseId = await CreateKnowledgeBaseAsync();
        using var form = new MultipartFormDataContent { { new StringContent("value"), "other" } };

        using var response = await client.PostAsync($"/api/v1/knowledge-bases/{knowledgeBaseId}/documents", form, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("document.file.required");
    }

    [Fact]
    public async Task Upload_UnknownKnowledgeBase_RespondsNotFound()
    {
        using var client = await MemberClientAsync();

        using var response = await UploadAsync(client, Guid.CreateVersion7(), "manual.pdf", _pdf);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("knowledgeBase.notFound");
    }

    [Fact]
    public async Task Upload_ArchivedKnowledgeBase_RespondsUnprocessable()
    {
        using var client = await MemberClientAsync();
        var knowledgeBaseId = await CreateKnowledgeBaseAsync(archived: true);

        using var response = await UploadAsync(client, knowledgeBaseId, "manual.pdf", _pdf);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("knowledgeBase.archived");
    }

    [Theory]
    [InlineData(1_024, "document.size.exceeded")]
    [InlineData(1_200_000, "request.tooLarge")]
    public async Task Upload_AboveTheConfiguredLimit_RespondsPayloadTooLarge(int size, string expectedKey)
    {
        await using var limited = Api.WithWebHostBuilder(builder => builder.UseSetting("Documents:MaxFileSizeBytes", "100"));
        var member = await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var client = limited.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(member));
        var knowledgeBaseId = await CreateKnowledgeBaseAsync();

        using var response = await UploadAsync(client, knowledgeBaseId, "large.pdf", [.. _pdf, .. new byte[size]]);

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe(expectedKey);
    }

    [Fact]
    public async Task Download_UploadedDocument_ReturnsAShortLivedLinkToTheOriginal()
    {
        using var client = await MemberClientAsync();
        var documentId = await UploadPdfAsync(client);

        using var response = await client.GetAsync($"/api/v1/documents/{documentId}/download", CancellationToken);
        var download = (await response.ReadEnvelopeAsync<DocumentDownloadResponse>(CancellationToken)).Data.ShouldNotBeNull();
        using var anonymous = Api.CreateApiClient();
        using var file = await anonymous.GetAsync(download.Url, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        download.Url.AbsolutePath.ShouldStartWith("/api/v1/files/");
        file.StatusCode.ShouldBe(HttpStatusCode.OK);
        file.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        file.Content.Headers.ContentDisposition!.DispositionType.ShouldBe("attachment");
        file.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        (await file.Content.ReadAsByteArrayAsync(CancellationToken)).ShouldBe(_pdf);
    }

    [Fact]
    public async Task Download_ForgedTicket_RespondsNotFound()
    {
        using var anonymous = Api.CreateApiClient();

        using var response = await anonymous.GetAsync("/api/v1/files/not-a-ticket", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListAndGet_UploadedDocument_ShowItsStatus()
    {
        using var client = await MemberClientAsync();
        var knowledgeBaseId = await CreateKnowledgeBaseAsync();
        using var upload = await UploadAsync(client, knowledgeBaseId, "Safety Manual.pdf", _pdf);
        var documentId = (await upload.ReadEnvelopeAsync<UploadDocumentResponse>(CancellationToken)).Data!.Id;

        using var list = await client.GetAsync($"/api/v1/knowledge-bases/{knowledgeBaseId}/documents?search=manual", CancellationToken);
        using var get = await client.GetAsync($"/api/v1/documents/{documentId}", CancellationToken);

        (await list.ReadEnvelopeAsync<PagedResponse<DocumentListItemResponse>>(CancellationToken)).Data!.Items.ShouldHaveSingleItem().Id.ShouldBe(documentId);
        var document = (await get.ReadEnvelopeAsync<GetDocumentResponse>(CancellationToken)).Data.ShouldNotBeNull();
        document.Status.ShouldBe(DocumentStatus.Uploaded);
        document.ContentType.ShouldBe("application/pdf");
    }

    [Fact]
    public async Task Delete_UploadedDocument_HidesItAndPublishesDocumentDeleted()
    {
        using var client = await MemberClientAsync();
        var documentId = await UploadPdfAsync(client);
        HttpResponseMessage? response = null;

        var session = await Api.Services.GetRequiredService<IHost>()
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ => response = await client.DeleteAsync($"/api/v1/documents/{documentId}", CancellationToken)));
        using var get = await client.GetAsync($"/api/v1/documents/{documentId}", CancellationToken);

        using (response)
        {
            response!.StatusCode.ShouldBe(HttpStatusCode.OK);
            session.Sent.SingleMessage<DocumentDeleted>().DocumentId.ShouldBe(documentId);
            get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task Reprocess_DocumentStillUploaded_RespondsUnprocessable()
    {
        using var client = await MemberClientAsync();
        var documentId = await UploadPdfAsync(client);

        using var response = await client.PostAsync($"/api/v1/documents/{documentId}/reprocess", content: null, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("document.status.invalidTransition");
    }

    private async Task<HttpClient> MemberClientAsync()
    {
        var member = await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        var client = Api.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(member));

        return client;
    }

    private async Task<Guid> CreateKnowledgeBaseAsync(bool archived = false)
    {
        var admin = await Api.SeedUserAsync($"admin-{Guid.CreateVersion7():N}@lexora.test", UserRole.Admin, TestUsers.Password);
        var builder = new KnowledgeBaseBuilder().WithName($"Base {Guid.CreateVersion7():N}").CreatedBy(admin.Id);
        var knowledgeBase = (archived ? builder.Archived() : builder).Build();

        await using var context = Api.CreateDbContext();
        context.KnowledgeBases.Add(knowledgeBase);
        await context.SaveChangesAsync(CancellationToken);

        return knowledgeBase.Id;
    }

    private async Task<Guid> UploadPdfAsync(HttpClient client)
    {
        using var response = await UploadAsync(client, await CreateKnowledgeBaseAsync(), "manual.pdf", _pdf);

        return (await response.ReadEnvelopeAsync<UploadDocumentResponse>(CancellationToken)).Data!.Id;
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid knowledgeBaseId, string fileName, byte[] content)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var form = new MultipartFormDataContent { { file, "file", fileName } };

        return client.PostAsync($"/api/v1/knowledge-bases/{knowledgeBaseId}/documents", form, CancellationToken);
    }

    private static byte[] Docx()
    {
        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in new[] { "[Content_Types].xml", "word/document.xml" })
            {
                using var writer = new StreamWriter(archive.CreateEntry(entry).Open());
                writer.Write("<xml/>");
            }
        }

        return buffer.ToArray();
    }
}
