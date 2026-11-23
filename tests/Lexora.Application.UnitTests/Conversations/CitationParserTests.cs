using Lexora.Application.Conversations.Prompts;
using Lexora.Application.Retrieval;
using Lexora.Application.Retrieval.Fusion;
using Lexora.Domain.Conversations;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class CitationParserTests
{
    private static readonly RagExcerpt[] _excerpts =
    [
        Excerpt(1, "Isole a área num raio de 50 metros.", page: 12),
        Excerpt(2, "Use luvas nitrílicas.", sheet: "Laboratório"),
        Excerpt(3, "Registre a ocorrência em 24 horas.", page: 3),
    ];

    private readonly CitationParser _parser = new();

    [Fact]
    public void Parse_Answer_CitesEachValidExcerptOnceInOrderOfFirstAppearance()
    {
        var citations = _parser.Parse("Registre em 24 horas [3]. Isole a área [1][3] e use luvas [2]. Veja também [9] e [0].", _excerpts);

        citations.Select(citation => citation.Index).ShouldBe([3, 1, 2]);
        citations[1].ShouldSatisfyAllConditions(
            citation => citation.DocumentName.ShouldBe("doc-1.pdf"),
            citation => citation.PageNumber.ShouldBe(12),
            citation => citation.Excerpt.ShouldBe("Isole a área num raio de 50 metros."),
            citation => citation.ChunkId.ShouldBe(_excerpts[0].Hit.ChunkId));
        citations[2].SheetName.ShouldBe("Laboratório");
    }

    [Fact]
    public void Parse_CommaSeparatedMarker_CitesEveryNumberInIt() =>
        _parser.Parse("Isole a área e registre [1, 3].", _excerpts).Select(citation => citation.Index).ShouldBe([1, 3]);

    [Theory]
    [InlineData("Não encontrei essa informação nos documentos.")]
    [InlineData("Apenas números inválidos [4] [12].")]
    [InlineData("Colchetes sem número [a] e [].")]
    public void Parse_AnswerWithoutValidMarkers_HasNoCitations(string answer) =>
        _parser.Parse(answer, _excerpts).ShouldBeEmpty();

    [Fact]
    public void Parse_LongExcerpt_IsShortenedAtAWordWithinTheLimit()
    {
        var longText = string.Join(' ', Enumerable.Repeat("procedimento", 60));

        var citation = _parser.Parse("Veja [1].", [Excerpt(1, longText)]).ShouldHaveSingleItem();

        citation.Excerpt.Length.ShouldBeLessThanOrEqualTo(Citation.MaxExcerptLength);
        citation.Excerpt.ShouldEndWith("procedimento…");
    }

    [Theory]
    [InlineData("Número enorme [99999999999999999999].")]
    [InlineData("Número negativo [-1].")]
    [InlineData("Número com sinal [+2].")]
    public void Parse_MarkerThatIsNotAPlainExcerptNumber_IsIgnored(string answer) =>
        _parser.Parse(answer, _excerpts).ShouldBeEmpty();

    [Fact]
    public void Parse_NestedBrackets_StillFindTheNumber() =>
        _parser.Parse("Veja [[2]].", _excerpts).ShouldHaveSingleItem().Index.ShouldBe(2);

    [Fact]
    public void Parse_Citation_KeepsTheScoreOfTheExcerpt() =>
        _parser.Parse("Veja [1].", _excerpts).ShouldHaveSingleItem().Score.ShouldBe(0.03);

    private static RagExcerpt Excerpt(int number, string content, int? page = null, string? sheet = null) =>
        new(number, new SearchHit(Guid.CreateVersion7(), Guid.CreateVersion7(), $"doc-{number}.pdf", content, page, sheet, null, 0.03, SearchMatch.Both));
}
