namespace Lexora.Application.Conversations.Services;

public static class NoContextAnswer
{
    public const string English = "I could not find this information in the documents of this knowledge base.";
    public const string Portuguese = "Não encontrei essa informação nos documentos desta base de conhecimento.";
    public const string Spanish = "No encontré esta información en los documentos de esta base de conocimiento.";

    private static readonly HashSet<string> _portugueseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "o", "os", "as", "do", "da", "dos", "das", "não", "é", "qual", "quais", "onde", "quando", "você", "são", "ao", "à", "também",
        "quanto", "quantos", "posso", "pode", "existe", "tem", "sobre", "isso", "essa", "esse", "deve", "devo", "seja",
    };

    private static readonly HashSet<string> _spanishWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "el", "los", "las", "del", "qué", "cuál", "cuáles", "cómo", "dónde", "cuándo", "es", "y", "está", "son", "hay", "puedo",
        "puede", "sobre", "esto", "esta", "este", "debo", "debe", "sea", "usted",
    };

    private static readonly HashSet<string> _englishWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "of", "what", "which", "how", "where", "when", "is", "are", "do", "does", "can", "should", "there", "about",
        "this", "that", "for", "and", "to", "who", "why", "must",
    };

    // The answer is fixed and the model is not called, so the language comes from common words of the question.
    public static string For(string question)
    {
        ArgumentNullException.ThrowIfNull(question);

        var words = question.Split([' ', '?', '!', '.', ',', ';', ':', '\n', '\t', '"', '(', ')'], StringSplitOptions.RemoveEmptyEntries);
        var portuguese = words.Count(_portugueseWords.Contains);
        var spanish = words.Count(_spanishWords.Contains);
        var english = words.Count(_englishWords.Contains);

        return portuguese >= spanish && portuguese > english ? Portuguese
            : spanish > portuguese && spanish > english ? Spanish
            : English;
    }
}
