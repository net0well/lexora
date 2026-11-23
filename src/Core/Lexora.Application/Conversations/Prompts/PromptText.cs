using System.Text.RegularExpressions;

namespace Lexora.Application.Conversations.Prompts;

public static partial class PromptText
{
    // Documents and questions are untrusted: if they could open or close the tags that delimit them, a document could end the
    // context early and append a fake question. Escaping only these tags keeps the rest of the text, such as "a < b" or HTML, intact.
    public static string Neutralize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return DelimiterTag().Replace(text, "&lt;$1$2");
    }

    [GeneratedRegex(@"<(\s*/?\s*)(documents|document|document_content|source|page|sheet|section|question|conversation)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DelimiterTag();
}
