using Lexora.Domain.KnowledgeBases;

namespace Lexora.Application.Abstractions.Processing;

public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(ExtractedDocument document, ChunkingSettings settings);
}
