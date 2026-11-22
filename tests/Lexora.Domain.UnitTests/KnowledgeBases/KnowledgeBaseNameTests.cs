using Lexora.Domain.KnowledgeBases;

namespace Lexora.Domain.UnitTests.KnowledgeBases;

public sealed class KnowledgeBaseNameTests
{
    [Fact]
    public void Create_NameWithSurroundingSpaces_Trims() => KnowledgeBaseName.Create("  Contracts ").Value.Value.ShouldBe("Contracts");

    [Theory]
    [InlineData(null, "knowledgeBase.name.required")]
    [InlineData("  ", "knowledgeBase.name.required")]
    [InlineData("ab", "knowledgeBase.name.tooShort")]
    public void Create_InvalidName_FailsWithMatchingKey(string? name, string expectedKey) =>
        KnowledgeBaseName.Create(name).Notifications.Single().Key.ShouldBe(expectedKey);

    [Fact]
    public void Create_OverMaximumLength_FailsAsTooLong() =>
        KnowledgeBaseName.Create(new string('n', KnowledgeBaseName.MaxLength + 1)).Notifications.Single().Key.ShouldBe("knowledgeBase.name.tooLong");

    [Theory]
    [InlineData(KnowledgeBaseName.MinLength)]
    [InlineData(KnowledgeBaseName.MaxLength)]
    public void Create_AtLengthBoundary_Succeeds(int length) =>
        KnowledgeBaseName.Create(new string('n', length)).IsSuccess.ShouldBeTrue();
}
