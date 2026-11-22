using System.Text;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing;
using Lexora.Infrastructure.DocumentProcessing.Extraction;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class TextExtractorsTests
{
    [Fact]
    public async Task PlainText_Paragraphs_AreGroupedIntoSectionsUpToTheLimit()
    {
        var paragraph = new string('a', 900);
        var text = string.Join("\r\n\r\n", paragraph, paragraph, paragraph);

        var document = await ExtractAsync(new PlainTextExtractor(), DocumentFormat.Txt, text);

        document.PageCount.ShouldBe(1);
        document.Sections.Select(section => section.Text.Length).ShouldBe([1802, 900]);
        document.Sections.ShouldAllBe(section => section.Title == null && section.PageNumber == null);
    }

    [Fact]
    public async Task PlainText_ByteOrderMark_IsNotPartOfTheText()
    {
        var bytes = (byte[])[0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("Olá mundo")];
        using var stream = new MemoryStream(bytes);

        var result = await new PlainTextExtractor().ExtractAsync(stream, DocumentFormat.Txt, TestContext.Current.CancellationToken);

        result.Value.Sections.Single().Text.ShouldBe("Olá mundo");
    }

    [Fact]
    public async Task Markdown_Headings_StartNewSectionsWithTheirTitle()
    {
        const string Text = """
            Intro without a heading.

            # Safety

            Wear gloves.

            ## Machines ##

            Turn them off.
            ```
            # not a heading inside code
            ```
            #### Deep heading stays in the body
            """;

        var document = await ExtractAsync(new MarkdownTextExtractor(), DocumentFormat.Markdown, Text);

        document.Sections.Select(section => section.Title).ShouldBe([null, "Safety", "Machines"]);
        document.Sections[1].Text.ShouldBe("Wear gloves.");
        document.Sections[2].Text.ShouldContain("# not a heading inside code");
        document.Sections[2].Text.ShouldContain("#### Deep heading stays in the body");
    }

    [Fact]
    public async Task Markdown_HeadingWithoutBody_ProducesNoSection() =>
        (await ExtractAsync(new MarkdownTextExtractor(), DocumentFormat.Markdown, "# Empty\n\n# Filled\nBody")).Sections
            .ShouldHaveSingleItem().Title.ShouldBe("Filled");

    [Theory]
    [InlineData(",")]
    [InlineData(";")]
    [InlineData("\t")]
    public async Task Csv_AnyDelimiter_TurnsRowsIntoHeaderValuePairs(string delimiter)
    {
        var text = string.Join('\n', $"name{delimiter}price{delimiter}notes", $"bolt{delimiter}1.5{delimiter}", $"nut{delimiter}0.5{delimiter}steel");

        var document = await ExtractAsync(Csv(), DocumentFormat.Csv, text);

        document.Sections.Single().Text.ShouldBe("name: bolt; price: 1.5\nname: nut; price: 0.5; notes: steel");
    }

    [Fact]
    public async Task Csv_QuotedFields_KeepDelimitersQuotesAndLineBreaks()
    {
        const string Text = "item,description\n\"A, B\",\"says \"\"hi\"\"\nand more\"";

        var document = await ExtractAsync(Csv(), DocumentFormat.Csv, Text);

        document.Sections.Single().Text.ShouldBe("item: A, B; description: says \"hi\"\nand more");
    }

    [Fact]
    public async Task Csv_MissingHeaderCells_AreNamedByColumn() =>
        (await ExtractAsync(Csv(), DocumentFormat.Csv, "a,\n1,2")).Sections.Single().Text.ShouldBe("a: 1; Column 2: 2");

    [Fact]
    public async Task Csv_MoreCellsThanTheLimit_FailsWithTheCellLimit()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("a,b\n1,2\n3,4"));

        var result = await Csv(maxCells: 5).ExtractAsync(stream, DocumentFormat.Csv, TestContext.Current.CancellationToken);

        result.Notifications.ShouldBe([ExtractionFailures.CellLimit]);
    }

    private static CsvTextExtractor Csv(int maxCells = 500_000) =>
        new(Options.Create(new DocumentProcessingOptions { MaxCells = maxCells }));

    private static async Task<ExtractedDocument> ExtractAsync(IFormatTextExtractor extractor, DocumentFormat format, string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        return (await extractor.ExtractAsync(stream, format, TestContext.Current.CancellationToken)).Value;
    }
}
