using Lexora.Application.Abstractions.Search;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;

namespace Lexora.Infrastructure.Persistence.Search;

public sealed class ChunkSearchIndex(LexoraDbContext context) : IChunkSearchIndex
{
    public async Task<IReadOnlyList<ChunkSearchCandidate>> VectorSearchAsync(
        Guid knowledgeBaseId,
        float[] queryEmbedding,
        string embeddingSpace,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);

        // The HNSW settings are transaction-scoped, so the query needs a transaction of its own.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync(SearchSql.VectorSettings, cancellationToken);

        var rows = await context.Database
            .SqlQueryRaw<ChunkSearchRow>(
                SearchSql.Vector,
                new NpgsqlParameter("query_embedding", new Vector(queryEmbedding)),
                new NpgsqlParameter("knowledge_base_id", knowledgeBaseId),
                new NpgsqlParameter("embedding_space", embeddingSpace),
                new NpgsqlParameter("limit", limit))
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return [.. rows.Select(ToCandidate)];
    }

    public async Task<IReadOnlyList<ChunkSearchCandidate>> KeywordSearchAsync(Guid knowledgeBaseId, string queryText, int limit, CancellationToken cancellationToken)
    {
        var rows = await context.Database
            .SqlQueryRaw<ChunkSearchRow>(
                SearchSql.Keyword,
                new NpgsqlParameter("query_text", queryText),
                new NpgsqlParameter("knowledge_base_id", knowledgeBaseId),
                new NpgsqlParameter("limit", limit))
            .ToListAsync(cancellationToken);

        return [.. rows.Select(ToCandidate)];
    }

    private static ChunkSearchCandidate ToCandidate(ChunkSearchRow row) =>
        new(row.ChunkId, row.DocumentId, row.DocumentName, row.Content, row.PageNumber, row.SheetName, row.SectionTitle, row.Score);
}
