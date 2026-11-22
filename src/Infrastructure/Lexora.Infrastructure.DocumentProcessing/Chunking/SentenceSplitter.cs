using System.Text.RegularExpressions;

namespace Lexora.Infrastructure.DocumentProcessing.Chunking;

public static partial class SentenceSplitter
{
    public static IEnumerable<TextSpan> Split(string text, TextSpan within)
    {
        foreach (var paragraph in Pieces(text, within, ParagraphBreak()))
        {
            foreach (var line in Pieces(text, paragraph, LineBreak()))
            {
                foreach (var sentence in Pieces(text, line, SentenceBreak()))
                {
                    yield return sentence;
                }
            }
        }
    }

    public static IEnumerable<TextSpan> Words(string text, TextSpan within) => Pieces(text, within, Whitespace());

    private static IEnumerable<TextSpan> Pieces(string text, TextSpan within, Regex separator)
    {
        var start = within.Start;

        foreach (Match match in separator.Matches(text[within.Start..within.End]))
        {
            var end = within.Start + match.Index;

            if (Trimmed(text, new TextSpan(start, end)) is { Length: > 0 } piece)
            {
                yield return piece;
            }

            start = end + match.Length;
        }

        if (Trimmed(text, new TextSpan(start, within.End)) is { Length: > 0 } last)
        {
            yield return last;
        }
    }

    private static TextSpan Trimmed(string text, TextSpan span)
    {
        var start = span.Start;
        var end = span.End;

        while (start < end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        return new TextSpan(start, end);
    }

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex ParagraphBreak();

    [GeneratedRegex(@"\n")]
    private static partial Regex LineBreak();

    [GeneratedRegex(@"(?<=[.!?…])\s+(?=[\p{Lu}\p{Nd}""“«(])")]
    private static partial Regex SentenceBreak();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
