namespace Lexora.Application.Conversations.Prompts;

public static class QuestionLanguageDetector
{
    private static readonly char[] _separators = [' ', '?', '!', '.', ',', ';', ':', '\n', '\r', '\t', '"', '(', ')', '¿', '¡'];

    private static readonly HashSet<string> _portugueseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "o", "os", "as", "do", "da", "dos", "das", "não", "é", "qual", "quais", "onde", "quando", "você", "são", "ao", "à", "também",
        "quanto", "quantos", "posso", "pode", "existe", "tem", "sobre", "isso", "essa", "esse", "deve", "devo", "seja", "em", "quem", "faço",
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

    // Small models drift to the language of the documents unless told the language by name, and common words are enough for that.
    public static QuestionLanguage Detect(string question)
    {
        ArgumentNullException.ThrowIfNull(question);

        var words = question.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
        var portuguese = words.Count(_portugueseWords.Contains);
        var spanish = words.Count(_spanishWords.Contains);
        var english = words.Count(_englishWords.Contains);

        return portuguese >= spanish && portuguese > english ? QuestionLanguage.Portuguese
            : spanish > portuguese && spanish > english ? QuestionLanguage.Spanish
            : english > portuguese && english > spanish ? QuestionLanguage.English
            : QuestionLanguage.Unknown;
    }
}
