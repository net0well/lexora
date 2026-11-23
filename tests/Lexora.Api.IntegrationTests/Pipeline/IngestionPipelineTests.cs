using System.Diagnostics;
using System.Net.Http.Headers;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Documents.GetDocument;
using Lexora.Application.Documents.UploadDocument;
using Lexora.Domain.Documents;
using Lexora.TestKit.Samples;

namespace Lexora.Api.IntegrationTests.Pipeline;

public sealed class IngestionPipelineTests(PipelineFixture pipeline) : IClassFixture<PipelineFixture>
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(90);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UploadedPdf_IsExtractedChunkedAndEmbeddedUntilIndexed()
    {
        var (client, knowledgeBaseId) = await pipeline.MemberWithKnowledgeBaseAsync(CancellationToken);
        using var _ = client;

        var documentId = await UploadAsync(client, knowledgeBaseId, SampleFiles.SafetyManualPdf, await SampleAsync(SampleFiles.SafetyManualPdf));
        var document = await WaitForAsync(client, documentId, document => document.Status == DocumentStatus.Indexed);

        document.PageCount.ShouldBe(3);
        document.ChunkCount.ShouldBeGreaterThan(0);
        document.EmbeddedChunkCount.ShouldBe(document.ChunkCount);
        document.IndexedAt.ShouldNotBeNull();
        var chunks = await pipeline.ChunksOfAsync(documentId, CancellationToken);
        chunks.Count.ShouldBe(document.ChunkCount);
        chunks.ShouldAllBe(chunk => chunk.ProcessingVersion == 1 && chunk.EmbeddingModel == "fake:fake-embedding:1024" && chunk.Embedding!.Length == 1024);
        chunks.Select(chunk => chunk.Location.PageNumber).Distinct().ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task Reprocess_IndexedDocument_ReplacesItsChunksWithTheNewVersion()
    {
        var (client, knowledgeBaseId) = await pipeline.MemberWithKnowledgeBaseAsync(CancellationToken);
        using var _ = client;
        var documentId = await UploadAsync(client, knowledgeBaseId, SampleFiles.VacationPolicyDocx, await SampleAsync(SampleFiles.VacationPolicyDocx));
        await WaitForAsync(client, documentId, document => document.Status == DocumentStatus.Indexed);

        using (var reprocess = await client.PostAsync($"/api/v1/documents/{documentId}/reprocess", content: null, CancellationToken))
        {
            reprocess.IsSuccessStatusCode.ShouldBeTrue();
        }

        var document = await WaitForAsync(client, documentId, document => document is { Status: DocumentStatus.Indexed, ProcessingVersion: 2 });

        var chunks = await pipeline.ChunksOfAsync(documentId, CancellationToken);
        chunks.Count.ShouldBe(document.ChunkCount);
        chunks.ShouldAllBe(chunk => chunk.ProcessingVersion == 2 && chunk.Embedding != null);
    }

    [Fact]
    public async Task CorruptedPdf_EndsFailedWithTheReason()
    {
        var (client, knowledgeBaseId) = await pipeline.MemberWithKnowledgeBaseAsync(CancellationToken);
        using var _ = client;
        var pdf = await SampleAsync(SampleFiles.SafetyManualPdf);

        var documentId = await UploadAsync(client, knowledgeBaseId, "damaged.pdf", pdf[..(pdf.Length / 2)]);
        var document = await WaitForAsync(client, documentId, document => document.Status == DocumentStatus.Failed);

        document.FailureReason.ShouldBe(ExtractionFailures.Unreadable.Message);
        (await pipeline.ChunksOfAsync(documentId, CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransientAiFailure_IsRetriedUntilTheDocumentIsIndexed()
    {
        var (client, knowledgeBaseId) = await pipeline.MemberWithKnowledgeBaseAsync(CancellationToken);
        using var _ = client;
        pipeline.Embeddings.FailNext(new AiProviderUnavailableException("fake", "fake", retryAfter: null, new HttpRequestException("Provider busy.")));

        var documentId = await UploadAsync(client, knowledgeBaseId, SampleFiles.FaqMarkdown, await SampleAsync(SampleFiles.FaqMarkdown));
        var document = await WaitForAsync(client, documentId, document => document.Status is DocumentStatus.Indexed or DocumentStatus.Failed);

        document.Status.ShouldBe(DocumentStatus.Indexed);
        document.EmbeddedChunkCount.ShouldBe(document.ChunkCount);
        pipeline.Embeddings.PendingFailures.ShouldBe(0);
    }

    [Fact]
    public async Task DeletedDocument_LosesItsStoredFilesAndChunks()
    {
        var (client, knowledgeBaseId) = await pipeline.MemberWithKnowledgeBaseAsync(CancellationToken);
        using var _ = client;
        var documentId = await UploadAsync(client, knowledgeBaseId, SampleFiles.MeetingNotesTxt, await SampleAsync(SampleFiles.MeetingNotesTxt));
        await WaitForAsync(client, documentId, document => document.Status == DocumentStatus.Indexed);
        var folder = Path.Combine(pipeline.StorageRoot, "knowledge-bases", knowledgeBaseId.ToString(), "documents", documentId.ToString());
        Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ShouldNotBeEmpty();

        using (var delete = await client.DeleteAsync($"/api/v1/documents/{documentId}", CancellationToken))
        {
            delete.IsSuccessStatusCode.ShouldBeTrue();
        }

        await EventuallyAsync(async () =>
            (!Directory.Exists(folder) || !Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Any())
            && (await pipeline.ChunksOfAsync(documentId, CancellationToken)).Count == 0);
    }

    private static async Task<byte[]> SampleAsync(string fileName) =>
        await File.ReadAllBytesAsync(SampleFiles.PathOf(fileName), CancellationToken);

    private static async Task<Guid> UploadAsync(HttpClient client, Guid knowledgeBaseId, string fileName, byte[] content)
    {
        using var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var form = new MultipartFormDataContent { { file, "file", fileName } };

        using var response = await client.PostAsync($"/api/v1/knowledge-bases/{knowledgeBaseId}/documents", form, CancellationToken);

        return (await response.ReadEnvelopeAsync<UploadDocumentResponse>(CancellationToken)).Data.ShouldNotBeNull().Id;
    }

    private static async Task<GetDocumentResponse> WaitForAsync(HttpClient client, Guid documentId, Func<GetDocumentResponse, bool> condition)
    {
        GetDocumentResponse? document = null;

        await EventuallyAsync(async () =>
        {
            using var response = await client.GetAsync($"/api/v1/documents/{documentId}", CancellationToken);
            document = (await response.ReadEnvelopeAsync<GetDocumentResponse>(CancellationToken)).Data;

            return document is not null && condition(document);
        });

        return document!;
    }

    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        var elapsed = Stopwatch.StartNew();

        while (!await condition())
        {
            elapsed.Elapsed.ShouldBeLessThan(_timeout);
            await Task.Delay(TimeSpan.FromMilliseconds(250), CancellationToken);
        }
    }
}
