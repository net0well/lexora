namespace Lexora.Infrastructure.Persistence.Search;

public static class SearchSql
{
    // pgvector 0.8 keeps scanning the HNSW graph when the knowledge base filter drops many neighbours.
    public const string VectorSettings = """
        SET TRANSACTION READ ONLY;
        SET LOCAL hnsw.ef_search = 100;
        SET LOCAL hnsw.iterative_scan = relaxed_order;
        """;

    public const string Vector = """
        SELECT c.id AS chunk_id, c.document_id, d.file_name AS document_name, c.content, c.page_number, c.sheet_name, c.section_title,
               1 - (c.embedding <=> @query_embedding) AS score
        FROM lexora.document_chunks c
        JOIN lexora.documents d ON d.id = c.document_id
        WHERE c.knowledge_base_id = @knowledge_base_id
          AND d.status = 'Indexed'
          AND d.deleted_at IS NULL
          AND c.processing_version = d.processing_version
          AND c.embedding IS NOT NULL
          AND c.embedding_model = @embedding_space
        ORDER BY c.embedding <=> @query_embedding
        LIMIT @limit
        """;

    public const string Keyword = """
        SELECT c.id AS chunk_id, c.document_id, d.file_name AS document_name, c.content, c.page_number, c.sheet_name, c.section_title,
               ts_rank_cd(c.search_vector, query) AS score
        FROM lexora.document_chunks c
        JOIN lexora.documents d ON d.id = c.document_id,
             websearch_to_tsquery('portuguese', @query_text) query
        WHERE c.knowledge_base_id = @knowledge_base_id
          AND d.status = 'Indexed'
          AND d.deleted_at IS NULL
          AND c.processing_version = d.processing_version
          AND c.search_vector @@ query
        ORDER BY score DESC
        LIMIT @limit
        """;
}
