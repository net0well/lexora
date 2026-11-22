using System.IO.Compression;
using System.Text;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing.Detection;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class FileSignatureInspectorTests
{
    private static readonly byte[] _pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%âã\n1 0 obj\n<<>>\nendobj\n%%EOF");

    public static TheoryData<string, byte[], DocumentFormat> Recognized => new()
    {
        { "manual.pdf", _pdf, DocumentFormat.Pdf },
        { "MANUAL.PDF", _pdf, DocumentFormat.Pdf },
        { "report.docx", OpenXml("word/document.xml"), DocumentFormat.Docx },
        { "budget.xlsx", OpenXml("xl/workbook.xml"), DocumentFormat.Xlsx },
        { "notes.txt", Encoding.UTF8.GetBytes("Plain text with acentuação."), DocumentFormat.Txt },
        { "notes.txt", [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("with a byte order mark")], DocumentFormat.Txt },
        { "readme.md", Encoding.UTF8.GetBytes("# Title\n\nBody"), DocumentFormat.Markdown },
        { "readme.markdown", Encoding.UTF8.GetBytes("# Title"), DocumentFormat.Markdown },
        { "table.csv", Encoding.UTF8.GetBytes("name;value\nalpha;1"), DocumentFormat.Csv },
    };

    public static TheoryData<string, byte[]> Rejected => new()
    {
        { "fake.pdf", Encoding.UTF8.GetBytes("this is not a pdf") },
        { "renamed.docx", OpenXml("xl/workbook.xml") },
        { "renamed.xlsx", OpenXml("word/document.xml") },
        { "plain.zip.docx", Zip("readme.txt") },
        { "broken.docx", [0x50, 0x4B, 0x03, 0x04, 0x00, 0x01] },
        { "binary.txt", [0x48, 0x69, 0xC3, 0x28] },
        { "nul.csv", [0x61, 0x00, 0x62] },
        { "document.pdf.exe", _pdf },
        { "no-extension", _pdf },
    };

    [Theory]
    [MemberData(nameof(Recognized))]
    public async Task DetectAsync_SignatureMatchingTheExtension_ReturnsTheFormat(string fileName, byte[] content, DocumentFormat expected)
    {
        using var stream = new MemoryStream(content);

        (await FileSignatureInspector.DetectAsync(stream, fileName, TestContext.Current.CancellationToken)).ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(Rejected))]
    public async Task DetectAsync_SignatureNotMatchingTheExtension_ReturnsNull(string fileName, byte[] content)
    {
        using var stream = new MemoryStream(content);

        (await FileSignatureInspector.DetectAsync(stream, fileName, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task DetectAsync_AnyResult_RewindsTheStream()
    {
        using var stream = new MemoryStream(OpenXml("word/document.xml"));

        await FileSignatureInspector.DetectAsync(stream, "report.docx", TestContext.Current.CancellationToken);

        stream.Position.ShouldBe(0);
    }

    [Fact]
    public async Task DetectAsync_TextLargerThanTheBuffer_ValidatesSequencesSplitAcrossReads()
    {
        var text = new string('a', 81919) + "ção";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        (await FileSignatureInspector.DetectAsync(stream, "long.txt", TestContext.Current.CancellationToken)).ShouldBe(DocumentFormat.Txt);
    }

    private static byte[] OpenXml(string mainPart) => Zip("[Content_Types].xml", mainPart);

    private static byte[] Zip(params string[] entries)
    {
        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in entries)
            {
                using var writer = new StreamWriter(archive.CreateEntry(entry).Open());
                writer.Write("<xml/>");
            }
        }

        return buffer.ToArray();
    }
}
