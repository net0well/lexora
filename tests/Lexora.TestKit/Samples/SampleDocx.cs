extern alias OpenXml;

using System.Globalization;
using OpenXml::DocumentFormat.OpenXml.ExtendedProperties;
using OpenXml::DocumentFormat.OpenXml.Packaging;
using OpenXml::DocumentFormat.OpenXml.Wordprocessing;
using Document = OpenXml::DocumentFormat.OpenXml.Wordprocessing.Document;
using WordprocessingDocumentType = OpenXml::DocumentFormat.OpenXml.WordprocessingDocumentType;

namespace Lexora.TestKit.Samples;

public static class SampleDocx
{
    public const string LocalizedHeadingStyleId = "Ttulo1";

    // Each line is a paragraph; "# " and "## " mark Heading1 and Heading2, "@ " marks a heading
    // whose style id is localized, and consecutive "| a | b" lines form a table.
    public static byte[] Create(int? pages, params string[] lines)
    {
        using var buffer = new MemoryStream();

        using (var document = WordprocessingDocument.Create(buffer, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body());
            main.AddNewPart<StyleDefinitionsPart>().Styles = new Styles(
                new Style(new StyleName { Val = "heading 1" }) { Type = StyleValues.Paragraph, StyleId = LocalizedHeadingStyleId });
            Write(main.Document.Body!, lines);

            if (pages is { } count)
            {
                document.AddExtendedFilePropertiesPart().Properties = new Properties(new Pages(count.ToString(CultureInfo.InvariantCulture)));
            }
        }

        return buffer.ToArray();
    }

    public static byte[] Create(params string[] lines) => Create(pages: null, lines);

    private static void Write(Body body, string[] lines)
    {
        Table? table = null;

        foreach (var line in lines)
        {
            if (line.StartsWith('|'))
            {
                table ??= body.AppendChild(new Table());
                var cells = line.Trim('|').Split('|').Select(cell => new TableCell(Paragraph(cell.Trim(), styleId: null)));
                table.AppendChild(new TableRow(cells));
                continue;
            }

            table = null;
            body.AppendChild(line switch
            {
                _ when line.StartsWith("# ", StringComparison.Ordinal) => Paragraph(line[2..], "Heading1"),
                _ when line.StartsWith("## ", StringComparison.Ordinal) => Paragraph(line[3..], "Heading2"),
                _ when line.StartsWith("@ ", StringComparison.Ordinal) => Paragraph(line[2..], LocalizedHeadingStyleId),
                _ => Paragraph(line, styleId: null),
            });
        }
    }

    private static Paragraph Paragraph(string text, string? styleId)
    {
        var paragraph = new Paragraph(new Run(new Text(text)));

        if (styleId is not null)
        {
            paragraph.PrependChild(new ParagraphProperties(new ParagraphStyleId { Val = styleId }));
        }

        return paragraph;
    }
}
