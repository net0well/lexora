using Lexora.Application.Abstractions.Search;
using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.Persistence.Search;
using Lexora.TestKit;
using Lexora.TestKit.Builders;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class KeywordSearchTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task KeywordSearchAsync_InflectedWords_MatchThroughPortugueseStemming()
    {
        var (knowledgeBaseId, document) = await SaveIndexedDocumentAsync();
        await SaveAsync(
            Chunk(document, 0, "Use luvas nitrílicas ao manusear solventes."),
            Chunk(document, 1, "Óculos de proteção são obrigatórios."));

        (await SearchAsync(knowledgeBaseId, "luva nitrílica")).Select(hit => hit.Content).ShouldBe(["Use luvas nitrílicas ao manusear solventes."]);
    }

    [Fact]
    public async Task KeywordSearchAsync_NormCode_FindsTheExactCodeOnly()
    {
        var (knowledgeBaseId, document) = await SaveIndexedDocumentAsync();
        await SaveAsync(
            Chunk(document, 0, "Instalações elétricas seguem a norma NR-10."),
            Chunk(document, 1, "Máquinas e equipamentos seguem a norma NR-12."));

        (await SearchAsync(knowledgeBaseId, "NR-10")).Select(hit => hit.Content).ShouldBe(["Instalações elétricas seguem a norma NR-10."]);
    }

    [Fact]
    public async Task KeywordSearchAsync_MoreOccurrences_RankHigher()
    {
        var (knowledgeBaseId, document) = await SaveIndexedDocumentAsync();
        await SaveAsync(
            Chunk(document, 0, "O vazamento deve ser comunicado."),
            Chunk(document, 1, "Em caso de vazamento, isole a área. Depois do vazamento, registre a ocorrência do vazamento."));

        var hits = await SearchAsync(knowledgeBaseId, "vazamento");

        hits.Select(hit => hit.Content[..7]).ShouldBe(["Em caso", "O vazam"]);
        hits[0].Score.ShouldBeGreaterThan(hits[1].Score);
    }

    [Fact]
    public async Task KeywordSearchAsync_WebSearchSyntax_ExcludesTheNegatedTermAndSearchesSectionTitles()
    {
        var (knowledgeBaseId, document) = await SaveIndexedDocumentAsync();
        await SaveAsync(
            Chunk(document, 0, "Isole a área do vazamento químico."),
            Chunk(document, 1, "Isole a área em caso de incêndio."),
            Chunk(document, 2, "Use o extintor adequado.", section: "Combate a incêndio"));

        (await SearchAsync(knowledgeBaseId, "isole -incêndio")).Select(hit => hit.Content).ShouldBe(["Isole a área do vazamento químico."]);
        (await SearchAsync(knowledgeBaseId, "combate")).Select(hit => hit.Content).ShouldBe(["Use o extintor adequado."]);
    }

    [Fact]
    public async Task KeywordSearchAsync_OnlyStopWords_FindsNothing()
    {
        var (knowledgeBaseId, document) = await SaveIndexedDocumentAsync();
        await SaveAsync(Chunk(document, 0, "A área de testes para o laboratório."));

        (await SearchAsync(knowledgeBaseId, "de para o")).ShouldBeEmpty();
    }

    [Fact]
    public async Task KeywordSearchAsync_ChunksThatAreNotCurrentlySearchable_AreLeftOut()
    {
        var (knowledgeBaseId, indexed) = await SaveIndexedDocumentAsync();
        var (_, otherBase) = await SaveIndexedDocumentAsync();
        var notIndexed = await SaveDocumentAsync(knowledgeBaseId, DocumentStatus.Chunked);
        var deleted = await SaveDocumentAsync(knowledgeBaseId, DocumentStatus.Indexed, document => document.Delete(TestClock.Now));
        await SaveAsync(
            Chunk(indexed, 0, "Extintor visível."),
            Chunk(indexed, 1, "Extintor de versão antiga.", processingVersion: 2),
            Chunk(otherBase, 0, "Extintor de outra base."),
            Chunk(notIndexed, 0, "Extintor ainda sem vetor."),
            Chunk(deleted, 0, "Extintor de documento excluído."));

        (await SearchAsync(knowledgeBaseId, "extintor")).Select(hit => hit.Content).ShouldBe(["Extintor visível."]);
    }

    private async Task<IReadOnlyList<ChunkSearchCandidate>> SearchAsync(Guid knowledgeBaseId, string query)
    {
        await using var context = Database.CreateContext();

        return await new ChunkSearchIndex(context).KeywordSearchAsync(knowledgeBaseId, query, limit: 10, CancellationToken);
    }

    private async Task<(Guid KnowledgeBaseId, Document Document)> SaveIndexedDocumentAsync()
    {
        var owner = await SaveUserAsync($"{Guid.CreateVersion7():N}@lexora.test");
        var knowledgeBase = new KnowledgeBaseBuilder().WithName($"Base {Guid.CreateVersion7():N}").CreatedBy(owner.Id).Build();
        var document = new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(owner.Id).InStatus(DocumentStatus.Indexed).Build();
        await SaveAsync(knowledgeBase, document);

        return (knowledgeBase.Id, document);
    }

    private async Task<Document> SaveDocumentAsync(Guid knowledgeBaseId, DocumentStatus status, Action<Document>? change = null)
    {
        await using var context = Database.CreateContext();
        var uploadedBy = context.KnowledgeBases.Single(knowledgeBase => knowledgeBase.Id == knowledgeBaseId).CreatedBy;
        var document = new DocumentBuilder().InKnowledgeBase(knowledgeBaseId).UploadedBy(uploadedBy).InStatus(status).Build();
        change?.Invoke(document);
        await SaveAsync(document);

        return document;
    }

    private static DocumentChunk Chunk(Document document, int index, string content, int processingVersion = 1, string? section = null) =>
        DocumentChunk.Create(document.KnowledgeBaseId, document.Id, processingVersion, index, content, 8, ChunkLocation.Create(1, null, section, 0, content.Length));
}
