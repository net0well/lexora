using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing;
using Lexora.TestKit.Samples;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class SampleFileExtractionTests : IDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection()
        .AddDocumentProcessingAdapter(new ConfigurationBuilder().Build())
        .BuildServiceProvider();

    private IDocumentTextExtractor Extractor => _services.GetRequiredService<IDocumentTextExtractor>();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static TheoryData<string, DocumentFormat?> Formats => new()
    {
        { SampleFiles.SafetyManualPdf, DocumentFormat.Pdf },
        { SampleFiles.VacationPolicyDocx, DocumentFormat.Docx },
        { SampleFiles.QuoteXlsx, DocumentFormat.Xlsx },
        { SampleFiles.FaqMarkdown, DocumentFormat.Markdown },
        { SampleFiles.MeetingNotesTxt, DocumentFormat.Txt },
        { SampleFiles.ContactsCsv, DocumentFormat.Csv },
        { SampleFiles.ContactsWindows1252Csv, null },
    };

    [Theory]
    [MemberData(nameof(Formats))]
    public async Task DetectAsync_SampleFile_IsRecognizedByItsContent(string fileName, DocumentFormat? expected)
    {
        await using var content = SampleFiles.Open(fileName);

        (await Extractor.DetectFormatAsync(content, fileName, CancellationToken)).ShouldBe(expected);
    }

    [Fact]
    public async Task ExtractAsync_BrowserPrintedPdf_ReturnsEachPageWithItsAccentedText()
    {
        var document = await ExtractAsync(SampleFiles.SafetyManualPdf, DocumentFormat.Pdf);

        document.PageCount.ShouldBe(3);
        document.Sections.Select(section => section.PageNumber).ShouldBe([1, 2, 3]);
        document.Sections[0].Text.ShouldContain("Manual de Segurança do Laboratório");
        document.Sections[0].Text.ShouldContain("Vazamentos químicos: isole a área num raio de 50 metros e acione a brigada de emergência.");
        document.Sections[0].Text.ShouldContain("Não utilize água em incêndios com sódio metálico.");
        document.Sections[1].Text.ShouldContain("Manuseio de solventes");
        document.Sections[1].Text.ShouldContain("Óculos de proteção e avental");
        document.Sections[2].Text.ShouldContain("Ação corretiva: registre a ocorrência no sistema em até 24 horas.");
    }

    [Fact]
    public async Task ExtractAsync_ExcelWorkbook_ReturnsEachSheetWithItsValues()
    {
        var document = await ExtractAsync(SampleFiles.QuoteXlsx, DocumentFormat.Xlsx);

        document.PageCount.ShouldBe(2);
        document.Sections.Select(section => section.SheetName).ShouldBe(["Resumo", "Itens"]);
        var summary = document.Sections[0].Text;
        summary.ShouldContain("Total geral");
        summary.ShouldContain("1000.6");
        var items = document.Sections[1].Text;
        items.ShouldContain("Item: Luvas nitrílicas; Quantidade: 10");
        items.ShouldContain("Item: Avental de PVC; Quantidade: 8");
        items.ShouldContain("Subtotal: 359.6");
    }

    [Fact]
    public async Task ExtractAsync_MarkdownFile_SplitsByHeadingAndKeepsListsCodeAndTables()
    {
        var document = await ExtractAsync(SampleFiles.FaqMarkdown, DocumentFormat.Markdown);

        document.Sections.Select(section => section.Title).ShouldBe(["Perguntas frequentes", "Como envio um arquivo?", "Quanto tempo leva a indexação?"]);
        document.Sections[1].Text.ShouldContain("Arquivos protegidos por senha não são aceitos.");
        document.Sections[2].Text.ShouldContain("curl -H \"Authorization: Bearer <token>\"");
        document.Sections[2].Text.ShouldContain("indexed | Pronto para busca");
    }

    [Fact]
    public async Task ExtractAsync_WindowsTextFile_UsesUnixLineBreaks()
    {
        var document = await ExtractAsync(SampleFiles.MeetingNotesTxt, DocumentFormat.Txt);

        var text = document.Sections.ShouldHaveSingleItem().Text;
        text.ShouldStartWith("Reunião de planejamento — 2 de março de 2026\n\nParticipantes: Ana, Bruno e Carla.");
        text.ShouldContain("R$ 12.500,00");
        text.ShouldNotContain('\r');
    }

    [Fact]
    public async Task ExtractAsync_ExcelCsvWithByteOrderMark_ReadsSemicolonsQuotesAndLineBreaks()
    {
        var document = await ExtractAsync(SampleFiles.ContactsCsv, DocumentFormat.Csv);

        var text = document.Sections.ShouldHaveSingleItem().Text;
        text.ShouldStartWith("Nome: Ana Lima; Cargo: Coordenadora; Ramal: 4321; Observações: Atende RH; folha");
        text.ShouldContain("Nome: João Côrtes; Cargo: Analista; Ramal: 4410; Observações: Plantão às\nsextas");
        text.ShouldNotContain('﻿');
    }

    [Theory]
    [InlineData(SampleFiles.SafetyManualPdf, DocumentFormat.Pdf)]
    [InlineData(SampleFiles.VacationPolicyDocx, DocumentFormat.Docx)]
    [InlineData(SampleFiles.QuoteXlsx, DocumentFormat.Xlsx)]
    public async Task ExtractAsync_SampleCutInHalf_FailsAsUnreadable(string fileName, DocumentFormat format)
    {
        var bytes = await File.ReadAllBytesAsync(SampleFiles.PathOf(fileName), CancellationToken);
        using var truncated = new MemoryStream(bytes[..(bytes.Length / 2)]);

        var result = await Extractor.ExtractAsync(truncated, format, CancellationToken);

        result.Notifications.ShouldBe([ExtractionFailures.Unreadable]);
    }

    public void Dispose() => _services.Dispose();

    private async Task<ExtractedDocument> ExtractAsync(string fileName, DocumentFormat format)
    {
        await using var content = SampleFiles.Open(fileName);

        return (await Extractor.ExtractAsync(content, format, CancellationToken)).Value;
    }
}
