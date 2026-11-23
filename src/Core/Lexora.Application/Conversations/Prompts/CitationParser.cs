using System.Globalization;
using System.Text.RegularExpressions;
using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.Prompts;

public sealed partial class CitationParser : ICitationParser
{
    private const char Ellipsis = '…';

    public IReadOnlyList<Citation> Parse(string answer, IReadOnlyList<RagExcerpt> excerpts)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(excerpts);

        var byNumber = excerpts.ToDictionary(excerpt => excerpt.Number);
        List<Citation> citations = [];
        HashSet<int> cited = [];

        foreach (Match marker in Markers().Matches(answer))
        {
            foreach (var part in marker.Groups["numbers"].Value.Split(','))
            {
                if (int.TryParse(part.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                    && byNumber.TryGetValue(number, out var excerpt)
                    && cited.Add(number))
                {
                    citations.Add(Citation.Create(
                        number,
                        excerpt.Hit.ChunkId,
                        excerpt.Hit.DocumentId,
                        excerpt.Hit.DocumentName,
                        excerpt.Hit.PageNumber,
                        excerpt.Hit.SheetName,
                        Shorten(excerpt.Hit.Content),
                        excerpt.Hit.Score));
                }
            }
        }

        return citations;
    }

    private static string Shorten(string content)
    {
        var text = content.Trim();

        if (text.Length <= Citation.MaxExcerptLength)
        {
            return text;
        }

        var cut = text[..(Citation.MaxExcerptLength - 1)];
        var lastSpace = cut.LastIndexOf(' ');

        return (lastSpace > Citation.MaxExcerptLength / 2 ? cut[..lastSpace] : cut).TrimEnd() + Ellipsis;
    }

    [GeneratedRegex(@"\[(?<numbers>\d+(?:\s*,\s*\d+)*)\]")]
    private static partial Regex Markers();
}
