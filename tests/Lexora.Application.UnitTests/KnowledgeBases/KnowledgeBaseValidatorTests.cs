using Lexora.Application.KnowledgeBases.CreateKnowledgeBase;
using Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;

namespace Lexora.Application.UnitTests.KnowledgeBases;

public sealed class KnowledgeBaseValidatorTests
{
    [Fact]
    public void Create_ValidInput_HasNoErrors() =>
        new CreateKnowledgeBaseValidator().Validate(new CreateKnowledgeBaseCommand("Safety Procedures", null, 800, 120)).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData("", "knowledgeBase.name.required")]
    [InlineData("ab", "knowledgeBase.name.tooShort")]
    public void Create_InvalidName_ReportsTheKey(string name, string expectedKey) =>
        new CreateKnowledgeBaseValidator().Validate(new CreateKnowledgeBaseCommand(name, null, null, null))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(expectedKey);

    [Fact]
    public void Create_TooLongValues_ReportsEachKey()
    {
        var result = new CreateKnowledgeBaseValidator().Validate(
            new CreateKnowledgeBaseCommand(new string('n', 121), new string('d', 1001), 199, -1));

        result.Errors.Select(error => error.ErrorCode).ShouldBe(
            ["knowledgeBase.name.tooLong", "knowledgeBase.description.tooLong", "knowledgeBase.chunking.invalid", "knowledgeBase.chunking.invalid"],
            ignoreOrder: true);
    }

    [Fact]
    public void Update_SameRulesAsCreate_RejectsMaxTokensAboveTheLimit() =>
        new UpdateKnowledgeBaseValidator().Validate(new UpdateKnowledgeBaseCommand(Guid.CreateVersion7(), "Safety", null, 2001, null))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("knowledgeBase.chunking.invalid");
}
