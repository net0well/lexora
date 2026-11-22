using Lexora.Domain.Documents;

namespace Lexora.Domain.UnitTests.Documents;

public sealed class FileNameTests
{
    [Theory]
    [InlineData("Relatório anual 2026.pdf", "Relatório anual 2026.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData(@"C:\Users\ana\contrato.docx", "contrato.docx")]
    [InlineData("re<p>o:rt|?*.pdf", "report.pdf")]
    [InlineData("a\u0000b\nc\u007F.txt", "abc.txt")]
    [InlineData("  notes.md.. ", "notes.md")]
    [InlineData("\"quoted\".csv", "quoted.csv")]
    public void Create_RawName_SanitizesIt(string raw, string expected) =>
        FileName.Create(raw).Value.Value.ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("folder/")]
    [InlineData("<>|")]
    [InlineData("...")]
    public void Create_NothingLeftAfterSanitizing_FailsWithInvalidKey(string? raw)
    {
        var result = FileName.Create(raw);

        result.IsFailure.ShouldBeTrue();
        result.Notifications.Single().Key.ShouldBe("document.fileName.invalid");
    }

    [Fact]
    public void Create_TooLongWithExtension_TruncatesKeepingExtension()
    {
        var name = FileName.Create($"{new string('a', 300)}.pdf").Value.Value;

        name.Length.ShouldBe(FileName.MaxLength);
        name.ShouldEndWith(".pdf");
    }

    [Fact]
    public void Create_TooLongWithoutExtension_TruncatesToMaximum() =>
        FileName.Create(new string('a', 300)).Value.Value.Length.ShouldBe(FileName.MaxLength);

    [Fact]
    public void Create_AtMaximumLength_KeepsName()
    {
        var raw = $"{new string('a', FileName.MaxLength - 4)}.pdf";

        FileName.Create(raw).Value.Value.ShouldBe(raw);
    }
}
