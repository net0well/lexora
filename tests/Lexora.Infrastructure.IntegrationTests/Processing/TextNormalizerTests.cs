using Lexora.Infrastructure.DocumentProcessing.Normalization;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData("exem-\nplo de texto", "exemplo de texto")]
    [InlineData("Lexora-\nAPI", "Lexora-\nAPI")]
    [InlineData("a   b\t\tc", "a b c")]
    [InlineData("  line one  \r\n  line two  ", "line one\nline two")]
    [InlineData("first\n\n\n\nsecond", "first\n\nsecond")]
    [InlineData("zero​width soft­hyphen", "zerowidth softhyphen")]
    [InlineData("bell\u0007 and nbsp", "bell and nbsp")]
    public void Normalize_Text_CleansLayoutArtifacts(string input, string expected) =>
        TextNormalizer.Normalize(input).ShouldBe(expected);

    [Fact]
    public void Normalize_DecomposedAccents_AreComposed() =>
        TextNormalizer.Normalize("café").ShouldBe("café");

    [Fact]
    public void Normalize_TableRows_KeepTheirLineBreaks() =>
        TextNormalizer.Normalize("Item: Bolt; Price: 1.5\nItem: Nut").ShouldBe("Item: Bolt; Price: 1.5\nItem: Nut");
}
