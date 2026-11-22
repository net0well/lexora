using Lexora.Infrastructure.Persistence.Queries;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class LikePatternTests
{
    [Theory]
    [InlineData("ana", "%ana%")]
    [InlineData("100%", @"%100\%%")]
    [InlineData("a_b", @"%a\_b%")]
    [InlineData(@"c:\temp", @"%c:\\temp%")]
    public void Contains_Value_EscapesWildcardsAndWrapsIt(string value, string expected) =>
        LikePattern.Contains(value).ShouldBe(expected);
}
