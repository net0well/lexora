using Lexora.Application.Conversations.Prompts;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class QuestionLanguageDetectorTests
{
    [Theory]
    [InlineData("O que fazer em vazamentos químicos?", QuestionLanguage.Portuguese)]
    [InlineData("Quem deve ser comunicado em vazamentos?", QuestionLanguage.Portuguese)]
    [InlineData("What should be done in a chemical spill?", QuestionLanguage.English)]
    [InlineData("¿Cuál es la política de vacaciones?", QuestionLanguage.Spanish)]
    [InlineData("¿Dónde está el extintor?", QuestionLanguage.Spanish)]
    [InlineData("NR-10?", QuestionLanguage.Unknown)]
    [InlineData("", QuestionLanguage.Unknown)]
    public void Detect_Question_UsesItsCommonWords(string question, QuestionLanguage expected) =>
        QuestionLanguageDetector.Detect(question).ShouldBe(expected);
}
