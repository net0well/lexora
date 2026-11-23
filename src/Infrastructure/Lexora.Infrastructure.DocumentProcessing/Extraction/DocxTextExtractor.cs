extern alias OpenXml;

using System.Text;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;
using OpenXml::DocumentFormat.OpenXml;
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

        foreach (var block in Blocks(body))
        {
            switch (block)
            {
                case Paragraph paragraph when IsHeading(paragraph, headingStyles):
                    Flush(sections, title, text);
                    title = TextOf(paragraph).Trim();
                    break;
                case Paragraph paragraph:
                    AppendLine(text, TextOf(paragraph));
                    break;
                case Table table:
                    foreach (var row in table.Elements<TableRow>())
                    {
                        AppendLine(text, string.Join(" | ", row.Elements<TableCell>().Select(CellText)));
                    }

                    break;
            }
        }

        Flush(sections, title, text);

        return sections;
    }

    private static IEnumerable<OpenXmlElement> Blocks(OpenXmlElement container)
    {
        foreach (var element in container.ChildElements)
        {
            if (element is Paragraph or Table)
            {
                yield return element;
                continue;
            }

            // Content controls and custom XML wrap ordinary paragraphs and tables.
            foreach (var nested in Blocks(element))
            {
                yield return nested;
            }
        }
    }

    private static string CellText(TableCell cell) =>
        string.Join(' ', Blocks(cell)
            .Select(block => TextOf(block).Replace('\n', ' ').Trim())
            .Where(value => value.Length > 0));

    private static string TextOf(OpenXmlElement element)
    {
        var text = new StringBuilder();
        AppendText(element, text);

        return text.ToString();
    }

    private static void AppendText(OpenXmlElement element, StringBuilder text)
    {
        foreach (var child in element.ChildElements)
        {
            switch (child)
            {
                case Text value:
                    text.Append(value.Text);
                    break;
                case TabChar or PositionalTab:
                    text.Append(' ');
                    break;
                case Break or CarriageReturn:
                    text.Append('\n');
                    break;
                case NoBreakHyphen:
                    text.Append('-');
                    break;
                case Paragraph:
                    text.Append('\n');
                    AppendText(child, text);
                    text.Append('\n');
                    break;
                case AlternateContent alternate:
                    // Word stores shapes twice, a modern choice and a legacy fallback, with the same text.
                    if (((OpenXmlElement?)alternate.GetFirstChild<AlternateContentChoice>() ?? alternate.GetFirstChild<AlternateContentFallback>()) is { } branch)
                    {
                        AppendText(branch, text);
                    }

                    break;
                case DeletedRun or MoveFromRun:
                    break;
                default:
                    AppendText(child, text);
                    break;
            }
        }
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
