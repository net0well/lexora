using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.KnowledgeBases;
using Lexora.Infrastructure.DocumentProcessing.Chunking;
using Lexora.Infrastructure.DocumentProcessing.Tokenization;
using Lexora.TestKit.Fakes;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class TokenAwareChunkerLimitTests
{
    private static readonly TiktokenTokenCounter _tokens = new();

    public static TheoryData<int, int> Limits => new()
    {
        { 200, 0 },
        { 200, 40 },
        { 512, 64 },
        { 800, 120 },
        { 2000, 300 },
    };

    [Theory]
    [MemberData(nameof(Limits))]
    public void Chunk_LinesOfATable_StayWithinTheLimitCountingTheLineBreaks(int maxTokens, int overlapTokens)
    {
        var text = string.Join('\n', Enumerable.Range(1, 600).Select(row =>
            $"Item: Luvas nitrílicas tamanho {row}; Quantidade: {row * 3}; Preço unitário: {row},50; Fornecedor: Proteção Total"));

        var chunks = Chunk(text, maxTokens, overlapTokens);

        chunks.Count.ShouldBeGreaterThan(1);
        chunks.ShouldAllBe(chunk => chunk.TokenCount <= maxTokens);
        chunks.ShouldAllBe(chunk => chunk.TokenCount == _tokens.Count(chunk.Content));
    }

    [Theory]
    [MemberData(nameof(Limits))]
    public void Chunk_MixedProse_CoversTheWholeTextInOrder(int maxTokens, int overlapTokens)
    {
        var text = Prose(paragraphs: 120);

        var chunks = Chunk(text, maxTokens, overlapTokens);

        chunks[0].StartOffset.ShouldBe(0);
        chunks[^1].EndOffset.ShouldBe(text.Length);

        for (var index = 1; index < chunks.Count; index++)
        {
            var (previous, current) = (chunks[index - 1], chunks[index]);
            current.EndOffset.ShouldBeGreaterThan(previous.EndOffset);
            current.StartOffset.ShouldBeGreaterThan(previous.StartOffset);

            if (current.StartOffset >= previous.EndOffset)
            {
                string.IsNullOrWhiteSpace(text[previous.EndOffset..current.StartOffset]).ShouldBeTrue();
            }
        }
    }

    [Theory]
    [InlineData(200, 40)]
    [InlineData(800, 120)]
    public void Chunk_Overlap_RepeatsWholeSentencesUpToTheConfiguredTokens(int maxTokens, int overlapTokens)
    {
        var text = Prose(paragraphs: 80);

        var chunks = Chunk(text, maxTokens, overlapTokens);

        chunks.Count(chunk => chunk.StartOffset > 0 && chunk.StartOffset < chunks[chunk.Index - 1].EndOffset).ShouldBeGreaterThan(chunks.Count / 2);

        for (var index = 1; index < chunks.Count; index++)
        {
            var start = chunks[index].StartOffset;
            char.IsWhiteSpace(text[start - 1]).ShouldBeTrue();
            (char.IsUpper(text[start]) || text[start] == '-').ShouldBeTrue();

            if (start < chunks[index - 1].EndOffset)
            {
                _tokens.Count(text[start..chunks[index - 1].EndOffset]).ShouldBeLessThanOrEqualTo(overlapTokens);
            }
        }
    }

    [Fact]
    public void Chunk_WordLongerThanTheLimit_IsCutIntoPiecesWithinTheLimit()
    {
        var random = new Random(42);
        var blob = new string([.. Enumerable.Range(0, 6_000).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"[random.Next(64)])]);
        var text = $"Chave pública: {blob} fim.";

        var chunks = Chunk(text, maxTokens: 200, overlapTokens: 0);

        chunks.Count.ShouldBeGreaterThan(1);
        chunks.ShouldAllBe(chunk => chunk.TokenCount <= 200);
        string.Concat(chunks.Select(chunk => chunk.Content.Replace(" ", string.Empty, StringComparison.Ordinal)))
            .ShouldBe(text.Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public void Chunk_OverlapLeavingNoRoomForTheNextSentence_IsDropped()
    {
        var text = $"{Words("Alpha", 150)}. {Words("Bravo", 15)}. {Words("Charlie", 190)}.";
        var chunker = new TokenAwareChunker(new WordTokenCounter());

        var chunks = chunker.Chunk(Document(text), Settings(200, 30));

        chunks.Select(chunk => chunk.Content[..5]).ShouldBe(["Alpha", "Charl"]);
    }

    private static List<TextChunk> Chunk(string text, int maxTokens, int overlapTokens) =>
        [.. new TokenAwareChunker(_tokens).Chunk(Document(text), Settings(maxTokens, overlapTokens))];

    private static ExtractedDocument Document(string text) => new([new ExtractedSection(null, 1, null, text)], PageCount: 1);

    private static ChunkingSettings Settings(int maxTokens, int overlapTokens) => ChunkingSettings.Create(maxTokens, overlapTokens).Value;

    private static string Words(string first, int count) => string.Join(' ', [first, .. Enumerable.Repeat("word", count - 1)]);

    private static string Prose(int paragraphs) =>
        string.Join("\n\n", Enumerable.Range(1, paragraphs).Select(paragraph => paragraph % 4 == 0
            ? $"Lista {paragraph}:\n- Isolar a área.\n- Avisar a brigada.\n- Registrar a ocorrência nº {paragraph}."
            : $"Parágrafo {paragraph}. Os resíduos químicos devem ser separados por classe antes do descarte. " +
              $"O responsável técnico confere a etiqueta, o volume e a data. Em caso de dúvida, consulte o manual na seção {paragraph % 7}!"));
}
