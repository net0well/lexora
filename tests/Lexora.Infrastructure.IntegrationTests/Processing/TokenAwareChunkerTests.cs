using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.KnowledgeBases;
using Lexora.Infrastructure.DocumentProcessing.Chunking;
using Lexora.TestKit.Fakes;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class TokenAwareChunkerTests
{
    private readonly TokenAwareChunker _chunker = new(new WordTokenCounter());

    [Fact]
    public void Chunk_ShortSection_IsOneChunkWithItsLocation()
    {
        var text = Sentences(60, wordsPerSentence: 5);
        var document = new ExtractedDocument([new ExtractedSection("Safety", 3, null, text)], PageCount: 3);

        var chunk = _chunker.Chunk(document, Settings(400, 40)).ShouldHaveSingleItem();

        chunk.Index.ShouldBe(0);
        chunk.Content.ShouldBe(text);
        chunk.TokenCount.ShouldBe(300);
        (chunk.PageNumber, chunk.SectionTitle, chunk.StartOffset, chunk.EndOffset).ShouldBe((3, "Safety", 0, text.Length));
    }

    [Fact]
    public void Chunk_LongSection_SplitsWithinTheLimitAndOverlapsWholeSentences()
    {
        var text = Sentences(200, wordsPerSentence: 10);
        var document = new ExtractedDocument([new ExtractedSection(null, 1, null, text)], PageCount: 1);

        var chunks = _chunker.Chunk(document, Settings(200, 30));

        chunks.Count.ShouldBeGreaterThan(5);
        chunks.ShouldAllBe(chunk => chunk.TokenCount <= 200);
        chunks.Select(chunk => chunk.Index).ShouldBe(Enumerable.Range(0, chunks.Count));

        for (var index = 1; index < chunks.Count; index++)
        {
            var overlap = chunks[index - 1].EndOffset - chunks[index].StartOffset;
            overlap.ShouldBeGreaterThan(0);
            chunks[index].Content.ShouldStartWith("Sentence");
            text[chunks[index].StartOffset..chunks[index - 1].EndOffset].Split(' ').Length.ShouldBeLessThanOrEqualTo(30);
        }
    }

    [Fact]
    public void Chunk_NoOverlap_ProducesAdjacentChunks()
    {
        var text = Sentences(100, wordsPerSentence: 10);
        var document = new ExtractedDocument([new ExtractedSection(null, 1, null, text)], PageCount: 1);

        var chunks = _chunker.Chunk(document, Settings(200, 0));

        for (var index = 1; index < chunks.Count; index++)
        {
            chunks[index].StartOffset.ShouldBeGreaterThan(chunks[index - 1].EndOffset);
        }
    }

    [Fact]
    public void Chunk_Sections_AreNeverCrossed()
    {
        var first = new ExtractedSection("One", 1, null, Sentences(30, wordsPerSentence: 10));
        var second = new ExtractedSection("Two", 2, null, Sentences(30, wordsPerSentence: 10));

        var chunks = _chunker.Chunk(new ExtractedDocument([first, second], PageCount: 2), Settings(1000, 100));

        chunks.Select(chunk => (chunk.SectionTitle, chunk.PageNumber)).ShouldBe([("One", 1), ("Two", 2)]);
    }

    [Fact]
    public void Chunk_SmallSection_IsMergedWithTheNextOneOfTheSamePage()
    {
        var heading = new ExtractedSection("Intro", 1, null, "Short opening line.");
        var body = new ExtractedSection("Details", 1, null, Sentences(20, wordsPerSentence: 10));
        var otherPage = new ExtractedSection(null, 2, null, "Tiny text on another page.");

        var chunks = _chunker.Chunk(new ExtractedDocument([heading, body, otherPage], PageCount: 2), Settings(1000, 100));

        chunks.Count.ShouldBe(2);
        chunks[0].SectionTitle.ShouldBe("Intro");
        chunks[0].Content.ShouldStartWith("Short opening line.");
        chunks[0].Content.ShouldContain("Sentence 1 ");
        chunks[1].PageNumber.ShouldBe(2);
    }

    [Fact]
    public void Chunk_SentenceLongerThanTheLimit_IsSplitByWords()
    {
        var text = string.Join(' ', Enumerable.Range(1, 450).Select(word => $"word{word}"));

        var chunks = _chunker.Chunk(new ExtractedDocument([new ExtractedSection(null, null, "Sheet1", text)], PageCount: 1), Settings(200, 20));

        chunks.ShouldAllBe(chunk => chunk.TokenCount <= 200 && chunk.SheetName == "Sheet1");
        string.Join(' ', chunks.Select(chunk => chunk.Content)).ShouldContain("word450");
    }

    private static ChunkingSettings Settings(int maxTokens, int overlapTokens) => ChunkingSettings.Create(maxTokens, overlapTokens).Value;

    private static string Sentences(int count, int wordsPerSentence) =>
        string.Join(' ', Enumerable.Range(1, count).Select(sentence =>
            $"Sentence {sentence} {string.Join(' ', Enumerable.Repeat("word", wordsPerSentence - 3))} end."));
}
