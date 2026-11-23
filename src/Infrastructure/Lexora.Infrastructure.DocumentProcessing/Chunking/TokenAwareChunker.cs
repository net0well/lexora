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
                var wordCount = tokens.Count(text[word.Start..word.End]);

                if (wordCount <= maxTokens)
                {
                    units.Add((word, wordCount));
                }
                else
                {
                    units.AddRange(Pieces(text, word, maxTokens));
                }
            }
        }

        return units;
    }

    private IEnumerable<(TextSpan Span, int Tokens)> Pieces(string text, TextSpan word, int maxTokens)
    {
        var start = word.Start;

        while (start < word.End)
        {
            var end = LongestFittingEnd(text, start, word.End, maxTokens);

            yield return (new TextSpan(start, end), tokens.Count(text[start..end]));

            start = end;
        }
    }

    private int LongestFittingEnd(string text, int start, int end, int maxTokens)
    {
        var low = start + 1;
        var high = end;

        while (low < high)
        {
            var middle = low + ((high - low + 1) / 2);

            if (tokens.Count(text[start..middle]) <= maxTokens)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }

        if (low < end && char.IsHighSurrogate(text[low - 1]))
        {
            low = low - 1 > start ? low - 1 : low + 1;
        }

        return low;
    }

    private IEnumerable<(TextSpan Span, int Tokens)> Pack(string text, List<(TextSpan Span, int Tokens)> units, ChunkingSettings settings)
    {
        var first = 0;

        while (first < units.Count)
        {
            var last = first;
            var total = units[first].Tokens;

            while (last + 1 < units.Count && total + JoinedCost(text, units, last + 1) <= settings.MaxTokens)
            {
                last++;
                total += JoinedCost(text, units, last);
            }

            var count = CountOf(text, units, first, last);

            // The estimate only approximates how the tokenizer joins neighbouring units; the real count decides.
            while (count > settings.MaxTokens && last > first)
            {
                last--;
                count = CountOf(text, units, first, last);
            }

            yield return (new TextSpan(units[first].Span.Start, units[last].Span.End), count);

            if (last + 1 >= units.Count)
            {
                yield break;
            }

            first = OverlapStart(text, units, first, last, settings);
        }
    }

    private static int OverlapStart(string text, List<(TextSpan Span, int Tokens)> units, int first, int last, ChunkingSettings settings)
    {
        var start = last + 1;

        // An overlap that leaves no room for the next unit would only repeat the previous chunk.
        while (start - 1 > first
            && Estimate(text, units, start - 1, last) <= settings.OverlapTokens
            && Estimate(text, units, start - 1, last + 1) <= settings.MaxTokens)
        {
            start--;
        }

        return start;
    }

    private static int Estimate(string text, List<(TextSpan Span, int Tokens)> units, int first, int last)
    {
        var total = units[first].Tokens;

        for (var index = first + 1; index <= last; index++)
        {
            total += JoinedCost(text, units, index);
        }

        return total;
    }

    // Spaces merge into the next word, but a line break stays a token of its own between units.
    private static int JoinedCost(string text, List<(TextSpan Span, int Tokens)> units, int index)
    {
        var previousEnd = units[index - 1].Span.End;
        var separator = text.AsSpan(previousEnd, units[index].Span.Start - previousEnd);

        return units[index].Tokens + (separator.Contains('\n') ? 1 : 0);
    }

    private int CountOf(string text, List<(TextSpan Span, int Tokens)> units, int first, int last) =>
        tokens.Count(text[units[first].Span.Start..units[last].Span.End]);
}
