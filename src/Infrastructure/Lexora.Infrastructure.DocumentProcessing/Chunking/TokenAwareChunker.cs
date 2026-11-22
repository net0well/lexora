using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.KnowledgeBases;

namespace Lexora.Infrastructure.DocumentProcessing.Chunking;

public sealed class TokenAwareChunker(ITokenCounter tokens) : ITextChunker
{
    public const int MinimumSectionTokens = 50;

    public IReadOnlyList<TextChunk> Chunk(ExtractedDocument document, ChunkingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(settings);

        List<TextChunk> chunks = [];

        foreach (var section in MergeSmallSections(document.Sections))
        {
            foreach (var (span, tokenCount) in Pack(section.Text, Units(section.Text, settings.MaxTokens), settings))
            {
                chunks.Add(new TextChunk(
                    chunks.Count,
                    section.Text[span.Start..span.End],
                    tokenCount,
                    section.PageNumber,
                    section.SheetName,
                    section.Title,
                    span.Start,
                    span.End));
            }
        }

        return chunks;
    }

    private List<ExtractedSection> MergeSmallSections(IReadOnlyList<ExtractedSection> sections)
    {
        List<ExtractedSection> merged = [];
        ExtractedSection? pending = null;

        foreach (var next in sections.Where(section => section.Text.Length > 0))
        {
            var section = next;

            if (pending is not null && pending.PageNumber == section.PageNumber && pending.SheetName == section.SheetName)
            {
                section = section with { Title = pending.Title ?? section.Title, Text = $"{pending.Text}\n\n{section.Text}" };
            }
            else if (pending is not null)
            {
                merged.Add(pending);
            }

            pending = null;

            if (tokens.Count(section.Text) < MinimumSectionTokens)
            {
                pending = section;
            }
            else
            {
                merged.Add(section);
            }
        }

        if (pending is not null)
        {
            merged.Add(pending);
        }

        return merged;
    }

    private List<(TextSpan Span, int Tokens)> Units(string text, int maxTokens)
    {
        List<(TextSpan, int)> units = [];

        foreach (var sentence in SentenceSplitter.Split(text, new TextSpan(0, text.Length)))
        {
            var count = tokens.Count(text[sentence.Start..sentence.End]);

            if (count <= maxTokens)
            {
                units.Add((sentence, count));
                continue;
            }

            foreach (var word in SentenceSplitter.Words(text, sentence))
            {
                units.Add((word, tokens.Count(text[word.Start..word.End])));
            }
        }

        return units;
    }

    private IEnumerable<(TextSpan Span, int Tokens)> Pack(string text, List<(TextSpan Span, int Tokens)> units, ChunkingSettings settings)
    {
        var first = 0;

        while (first < units.Count)
        {
            var last = first;
            var total = units[first].Tokens;

            while (last + 1 < units.Count && total + units[last + 1].Tokens <= settings.MaxTokens)
            {
                last++;
                total += units[last].Tokens;
            }

            var span = new TextSpan(units[first].Span.Start, units[last].Span.End);

            yield return (span, tokens.Count(text[span.Start..span.End]));

            if (last + 1 >= units.Count)
            {
                yield break;
            }

            first = OverlapStart(units, first, last, settings.OverlapTokens);
        }
    }

    private static int OverlapStart(List<(TextSpan Span, int Tokens)> units, int first, int last, int overlapTokens)
    {
        var start = last + 1;
        var overlap = 0;

        while (start - 1 > first && overlap + units[start - 1].Tokens <= overlapTokens)
        {
            start--;
            overlap += units[start].Tokens;
        }

        return start;
    }
}
