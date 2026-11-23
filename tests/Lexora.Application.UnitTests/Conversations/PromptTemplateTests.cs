using Lexora.Application.Conversations.Prompts;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class PromptTemplateTests
{
    [Theory]
    [InlineData("answer-system")]
    [InlineData("answer-user")]
    [InlineData("rewrite-system")]
    [InlineData("rewrite-user")]
    public void Load_EmbeddedTemplate_HasMarkdownTextWithUnixLineEndings(string name)
    {
        var template = PromptTemplate.Load(name);

        template.Text.ShouldNotBeNullOrWhiteSpace();
        template.Text.ShouldNotContain('\r');
        template.Text.ShouldBe(template.Text.Trim());
    }

    [Fact]
    public void Load_UnknownTemplate_Throws() =>
        Should.Throw<InvalidOperationException>(() => PromptTemplate.Load("missing")).Message.ShouldContain("'missing'");

    [Fact]
    public void Render_Values_FillsEveryPlaceholder()
    {
        var text = PromptTemplate.Load("answer-user").Render(new Dictionary<string, string>
        {
            ["documents"] = "<document index=\"1\">…</document>",
            ["question"] = "Qual o raio?",
            ["language"] = "Portuguese",
        });

        text.ShouldNotContain("{{");
        text.ShouldContain("Answer the question above in Portuguese.");
    }

    [Fact]
    public void Render_ValueWithAPlaceholder_IsNotExpandedAgain()
    {
        var text = PromptTemplate.Load("answer-user").Render(new Dictionary<string, string>
        {
            ["documents"] = "Texto com {{question}} e {{language}}.",
            ["question"] = "Qual o raio?",
            ["language"] = "Portuguese",
        });

        text.ShouldContain("Texto com {{question}} e {{language}}.");
    }

    [Fact]
    public void Render_MissingValue_Throws() =>
        Should.Throw<InvalidOperationException>(() => PromptTemplate.Load("answer-user").Render(new Dictionary<string, string> { ["question"] = "Qual o raio?" }))
            .Message.ShouldContain("'documents'");
}
