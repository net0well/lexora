using Lexora.Infrastructure.DocumentProcessing.Tokenization;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class TiktokenTokenCounterTests
{
    private readonly TiktokenTokenCounter _counter = new();

    [Theory]
    [InlineData("", 0)]
    [InlineData("hello world", 2)]
    public void Count_KnownText_MatchesTheEncoding(string text, int expected) => _counter.Count(text).ShouldBe(expected);

    [Fact]
    public void Count_LongerText_GrowsWithTheContent()
    {
        var sentence = _counter.Count("Use luvas e óculos de proteção ao operar a máquina.");

        _counter.Count(string.Join(' ', Enumerable.Repeat("Use luvas e óculos de proteção ao operar a máquina.", 10))).ShouldBeGreaterThan(sentence * 9);
    }
}
