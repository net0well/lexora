using Lexora.Domain.Conversations;

namespace Lexora.Domain.UnitTests.Conversations;

public sealed class TokenUsageTests
{
    [Fact]
    public void Create_Counts_ExposesTotal()
    {
        var usage = TokenUsage.Create(1200, 85);

        usage.InputTokens.ShouldBe(1200);
        usage.OutputTokens.ShouldBe(85);
        usage.TotalTokens.ShouldBe(1285);
    }

    [Fact]
    public void None_HasNoTokens() => TokenUsage.None.TotalTokens.ShouldBe(0);

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Create_NegativeCount_Throws(int inputTokens, int outputTokens) =>
        Should.Throw<ArgumentOutOfRangeException>(() => TokenUsage.Create(inputTokens, outputTokens));

    [Fact]
    public void Equals_SameCounts_ReturnsTrue() => TokenUsage.Create(0, 0).ShouldBe(TokenUsage.None);
}
