extern alias OpenXml;

using System.Text;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;
using OpenXml::DocumentFormat.OpenXml.Packaging;
using OpenXml::DocumentFormat.OpenXml.Wordprocessing;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public sealed class DocxTextExtractor(IOptions<DocumentProcessingOptions> options) : IFormatTextExtractor
{
    private static readonly HashSet<string> _headingNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "heading 1", "heading 2", "heading 3", "Heading1", "Heading2", "Heading3",
    };

    public IReadOnlyCollection<DocumentFormat> Formats { get; } = [DocumentFormat.Docx];

    public async Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken)
    {
        await using var seekable = await SeekableContent.FromAsync(content, cancellationToken);

        try
        {
            using var document = WordprocessingDocument.Open(seekable.Stream, isEditable: false);
            var pageCount = PageCountOf(document);

            if (pageCount > options.Value.MaxPages)
            {
                return Result.Failure<ExtractedDocument>(ExtractionFailures.PageLimit);
            }

            var body = document.MainDocumentPart?.Document?.Body;

            return body is null
                ? Result.Failure<ExtractedDocument>(ExtractionFailures.Unreadable)
                : new ExtractedDocument(Sections(body, HeadingStyleIds(document)), pageCount);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The OpenXML SDK reports damaged packages through several exception types; all mean an unreadable file.
            return Result.Failure<ExtractedDocument>(ExtractionFailures.Unreadable);
        }
    }

    private static List<ExtractedSection> Sections(Body body, HashSet<string> headingStyles)
    {
        List<ExtractedSection> sections = [];
        string? title = null;
        var text = new StringBuilder();

        foreach (var element in body.ChildElements)
        {
            switch (element)
            {
                case Paragraph paragraph when IsHeading(paragraph, headingStyles):
                    Flush(sections, title, text);
                    title = paragraph.InnerText.Trim();
                    break;
                case Paragraph paragraph:
                    AppendLine(text, paragraph.InnerText);
                    break;
                case Table table:
                    foreach (var row in table.Elements<TableRow>())
                    {
                        AppendLine(text, string.Join(" | ", row.Elements<TableCell>().Select(cell => cell.InnerText.Trim())));
                    }

                    break;
            }
        }

        Flush(sections, title, text);

        return sections;
    }

    private static bool IsHeading(Paragraph paragraph, HashSet<string> headingStyles) =>
        paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value is { } styleId && headingStyles.Contains(styleId);

    private static HashSet<string> HeadingStyleIds(WordprocessingDocument document)
    {
        var headingIds = new HashSet<string>(_headingNames, StringComparer.OrdinalIgnoreCase);
        var styles = document.MainDocumentPart?.StyleDefinitionsPart?.Styles?.Elements<Style>() ?? [];

        foreach (var style in styles)
        {
            if (style.StyleId?.Value is { } id && style.StyleName?.Val?.Value is { } name && _headingNames.Contains(name))
            {
                headingIds.Add(id);
            }
        }

        return headingIds;
    }

    private static int PageCountOf(WordprocessingDocument document) =>
        int.TryParse(document.ExtendedFilePropertiesPart?.Properties?.Pages?.Text, out var pages) && pages > 0 ? pages : 1;

    private static void AppendLine(StringBuilder text, string line)
    {
        if (line.Trim().Length > 0)
        {
            text.Append(line.Trim()).Append('\n');
        }
    }

    private static void Flush(List<ExtractedSection> sections, string? title, StringBuilder text)
    {
        var value = text.ToString().Trim();
        text.Clear();

        if (value.Length > 0)
        {
            sections.Add(new ExtractedSection(title, null, null, value));
        }
    }
}
