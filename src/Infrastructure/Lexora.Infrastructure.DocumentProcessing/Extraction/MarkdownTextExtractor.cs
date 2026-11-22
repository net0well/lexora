using System.Text;
using System.Text.RegularExpressions;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Shared.Results;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public sealed partial class MarkdownTextExtractor : IFormatTextExtractor
{
    public IReadOnlyCollection<DocumentFormat> Formats { get; } = [DocumentFormat.Markdown];

    public async Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken)
    {
        var text = await TextReading.ReadAllAsync(content, cancellationToken);
        List<ExtractedSection> sections = [];
        string? title = null;
        var body = new StringBuilder();
        var insideFence = false;

        foreach (var line in text.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                insideFence = !insideFence;
            }

            var heading = insideFence ? null : HeadingPattern().Match(line);

            if (heading is { Success: true })
            {
                Flush(sections, title, body);
                title = heading.Groups["title"].Value.Trim();
                continue;
            }

            body.Append(line).Append('\n');
        }

        Flush(sections, title, body);

        return new ExtractedDocument(sections, PageCount: 1);
    }

    private static void Flush(List<ExtractedSection> sections, string? title, StringBuilder body)
    {
        var text = body.ToString().Trim();
        body.Clear();

        if (text.Length > 0)
        {
            sections.Add(new ExtractedSection(title, null, null, text));
        }
    }

    [GeneratedRegex(@"^\s{0,3}#{1,3}\s+(?<title>.+?)\s*#*\s*$")]
    private static partial Regex HeadingPattern();
}
