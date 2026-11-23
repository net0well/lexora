using System.Security.Cryptography;
using System.Text;
using Lexora.Application.Abstractions.Ai;

namespace Lexora.Application.Retrieval.Services;

public static class QueryEmbeddingCacheKey
{
    // The space is part of the key so vectors from another model or size are never reused.
    public static string For(EmbeddingSpace space, string query)
    {
        ArgumentNullException.ThrowIfNull(space);

        return $"query-embedding:{space.Key}:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(query)))}";
    }
}
