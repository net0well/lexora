using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Common.Options;
using Lexora.Application.Documents;
using Lexora.Application.Ingestion;
using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Lexora.Application.UnitTests.Ingestion;

internal sealed class IngestionHarness
{
    public IngestionHarness(int embeddingBatchSize = 32)
    {
        Storage = new InMemoryFileStorage(Time);
        Chunker.Chunk(Arg.Any<ExtractedDocument>(), Arg.Any<ChunkingSettings>())
            .Returns(call => [.. call.Arg<ExtractedDocument>().Sections.Select((section, index) =>
                new TextChunk(index, section.Text, 10, section.PageNumber, section.SheetName, section.Title, 0, section.Text.Length))]);
        Service = new IngestionService(
            Documents,
            KnowledgeBases,
            Chunks,
            Extractor,
            Chunker,
            Embeddings,
            Storage,
            Options.Create(new IngestionOptions { EmbeddingBatchSize = embeddingBatchSize }),
            Time,
            NullLogger<IngestionService>.Instance);
    }

    public FakeTimeProvider Time { get; } = new(TestClock.Now);

    public InMemoryDocumentRepository Documents { get; } = new();

    public InMemoryKnowledgeBaseRepository KnowledgeBases { get; } = new();

    public InMemoryDocumentChunkRepository Chunks { get; } = new();

    public IDocumentTextExtractor Extractor { get; } = Substitute.For<IDocumentTextExtractor>();

    public ITextChunker Chunker { get; } = Substitute.For<ITextChunker>();

    public FakeEmbeddingModel Embeddings { get; } = new();

    public InMemoryFileStorage Storage { get; }

    public IngestionService Service { get; }

    public async Task<Document> AddUploadedAsync()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().Build();
        await KnowledgeBases.AddAsync(knowledgeBase, CancellationToken.None);

        var document = new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).Build();
        document.ClearDomainEvents();
        await Documents.AddAsync(document, CancellationToken.None);

        using var original = new MemoryStream([1, 2, 3]);
        await Storage.SaveAsync(document.StorageKey, original, document.ContentType, CancellationToken.None);

        return document;
    }

    public async Task<Document> AddChunkedAsync(int chunkCount)
    {
        var sections = Enumerable.Range(0, chunkCount).Select(index => new ExtractedSection(index % 2 == 0 ? "Safety" : null, 1, null, $"chunk {index}"));
        var document = await AddExtractedAsync(new ExtractedDocument([.. sections], PageCount: 1));
        await Service.ChunkAsync(document.Id, document.ProcessingVersion, CancellationToken.None);
        document.ClearDomainEvents();

        return document;
    }

    public async Task<Document> AddExtractedAsync(ExtractedDocument extracted)
    {
        var document = await AddUploadedAsync();
        var key = DocumentStorageKeys.ExtractedOf(document.KnowledgeBaseId, document.Id, document.ProcessingVersion);

        await using (var json = await ExtractedDocumentJson.SerializeAsync(extracted, CancellationToken.None))
        {
            await Storage.SaveAsync(key, json, ExtractedDocumentJson.ContentType, CancellationToken.None);
        }

        document.CompleteExtraction(document.ProcessingVersion, extracted.PageCount, key, TestClock.Now);
        document.ClearDomainEvents();

        return document;
    }
}
