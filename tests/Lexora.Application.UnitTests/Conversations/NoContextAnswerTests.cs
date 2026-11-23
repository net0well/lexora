using Lexora.Application.Conversations.Services;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class NoContextAnswerTests
{
    [Theory]
    [InlineData("Quais são os EPIs obrigatórios no laboratório?", NoContextAnswer.Portuguese)]
    [InlineData("Onde fica o extintor?", NoContextAnswer.Portuguese)]
    [InlineData("What is the evacuation route?", NoContextAnswer.English)]
    [InlineData("¿Dónde está el extintor?", NoContextAnswer.Spanish)]
    [InlineData("NR-10?", NoContextAnswer.English)]
    [InlineData("", NoContextAnswer.English)]
    public void For_Question_UsesTheLanguageOfItsCommonWords(string question, string expected) =>
        NoContextAnswer.For(question).ShouldBe(expected);
}
