using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.Persistence;
using Lexora.TestKit.Builders;
using Npgsql;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class SchemaTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task EmbeddingColumn_Type_IsVectorWithConfiguredDimensions() =>
        (await ScalarAsync<string>(
            """
            SELECT format_type(atttypid, atttypmod)
            FROM pg_attribute
            WHERE attrelid = 'lexora.document_chunks'::regclass AND attname = 'embedding'
            """))
        .ShouldBe($"vector({SchemaHelper.EmbeddingDimensions})");

    [Fact]
    public async Task EmbeddingIndex_Definition_IsHnswWithCosineOperatorsAndTuning()
    {
        var definition = await IndexDefinitionAsync("ix_document_chunks_embedding");

        definition.ShouldContain("USING hnsw (embedding vector_cosine_ops)");
        definition.ShouldContain("m='16'");
        definition.ShouldContain("ef_construction='64'");
    }

    [Fact]
    public async Task SearchVectorIndex_Definition_IsGin() =>
        (await IndexDefinitionAsync("ix_document_chunks_search_vector")).ShouldContain("USING gin (search_vector)");

    [Fact]
    public async Task SearchVectorColumn_Definition_IsGeneratedFromSectionTitleAndContent()
    {
        var expression = await GenerationExpressionAsync("document_chunks", "search_vector");

        expression.ShouldContain("to_tsvector('portuguese'");
        expression.ShouldContain("section_title");
        expression.ShouldContain("content");
    }

    [Fact]
    public async Task SearchVector_SavedChunk_MatchesAnotherInflectionOfTheWordInPortuguese()
    {
        var owner = await SaveUserAsync();
        var knowledgeBase = new KnowledgeBaseBuilder().CreatedBy(owner.Id).Build();
        var document = new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(owner.Id).InStatus(DocumentStatus.Chunked).Build();
        var chunk = DocumentChunk.Create(
            knowledgeBase.Id,
            document.Id,
            1,
            0,
            "O pagamento vence em trinta dias após a emissão da nota.",
            12,
            ChunkLocation.Create(2, null, "Condições comerciais", 0, 58));
        await SaveAsync(knowledgeBase, document, chunk);

        var matches = await ScalarAsync<long>(
            "SELECT count(*) FROM lexora.document_chunks WHERE search_vector @@ websearch_to_tsquery('portuguese', 'pagamentos comerciais')");

        matches.ShouldBe(1);
    }

    [Fact]
    public async Task VectorExtension_Schema_IsPublic() =>
        (await ScalarAsync<string>("SELECT extnamespace::regnamespace::text FROM pg_extension WHERE extname = 'vector'")).ShouldBe("public");

    [Fact]
    public async Task DocumentHashIndex_Definition_IsUniqueOnlyForActiveDocuments()
    {
        var definition = await IndexDefinitionAsync("ix_documents_knowledge_base_id_content_hash");

        definition.ShouldStartWith("CREATE UNIQUE INDEX");
        definition.ShouldEndWith("WHERE (deleted_at IS NULL)");
    }

    [Fact]
    public async Task KnowledgeBaseNameIndex_Definition_IsUniqueOnGeneratedLowercaseName()
    {
        (await IndexDefinitionAsync("ix_knowledge_bases_normalized_name")).ShouldStartWith("CREATE UNIQUE INDEX");

        (await GenerationExpressionAsync("knowledge_bases", "normalized_name")).ShouldContain("lower(");
    }

    private async Task<string> GenerationExpressionAsync(string table, string column) =>
        (await ScalarAsync<string>(
            $"""
            SELECT generation_expression
            FROM information_schema.columns
            WHERE table_schema = 'lexora' AND table_name = '{table}' AND column_name = '{column}' AND is_generated = 'ALWAYS'
            """))
        .ShouldNotBeNull();

    private async Task<string> IndexDefinitionAsync(string indexName) =>
        (await ScalarAsync<string>($"SELECT indexdef FROM pg_indexes WHERE schemaname = 'lexora' AND indexname = '{indexName}'"))
        .ShouldNotBeNull();

    private async Task<T?> ScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(Database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        return (T?)await command.ExecuteScalarAsync(CancellationToken);
    }
}
