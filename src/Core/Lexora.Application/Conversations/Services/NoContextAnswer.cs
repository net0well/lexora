using Lexora.Application.Conversations.Prompts;

namespace Lexora.Application.Conversations.Services;

public static class NoContextAnswer
{
    public const string English = "I could not find this information in the documents of this knowledge base.";
    public const string Portuguese = "Não encontrei essa informação nos documentos desta base de conhecimento.";
    public const string Spanish = "No encontré esta información en los documentos de esta base de conocimiento.";

    public static string For(string question) => QuestionLanguageDetector.Detect(question) switch
    {
        QuestionLanguage.Portuguese => Portuguese,
        QuestionLanguage.Spanish => Spanish,
        _ => English,
    };
}
