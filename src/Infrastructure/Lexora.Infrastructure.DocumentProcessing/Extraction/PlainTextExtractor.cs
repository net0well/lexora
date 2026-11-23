using System.Text;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Shared.Results;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public sealed class PlainTextExtractor : IFormatTextExtractor
{
    public const int SectionCharacters = 2_000;

    public IReadOnlyCollection<DocumentFormat> Formats { get; } = [DocumentFormat.Txt];

    public async Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken)
    {
        var text = await TextReading.ReadAllAsync(content, cancellationToken);
        List<ExtractedSection> sections = [];
        var current = new StringBuilder();

        foreach (var paragraph in TextReading.Paragraphs(text))
        {
            if (current.Length > 0 && current.Length + paragraph.Length + 2 > SectionCharacters)
            {
                sections.Add(new ExtractedSection(null, null, null, current.ToString()));
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append("\n\n");
            }

            current.Append(paragraph);
        }

        if (current.Length > 0)
        {
            sections.Add(new ExtractedSection(null, null, null, current.ToString()));
        }

        return new ExtractedDocument(sections, PageCount: 1);
    }
}
