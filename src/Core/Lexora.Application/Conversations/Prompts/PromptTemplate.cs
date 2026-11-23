using System.Text;
using System.Text.RegularExpressions;

namespace Lexora.Application.Conversations.Prompts;

public sealed partial class PromptTemplate
{
    private PromptTemplate(string name, string text)
    {
        Name = name;
        Text = text;
    }

    public string Name { get; }

    public string Text { get; }

    public static PromptTemplate Load(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var stream = typeof(PromptTemplate).Assembly.GetManifestResourceStream($"{typeof(PromptTemplate).Namespace}.Templates.{name}.md")
            ?? throw new InvalidOperationException($"The prompt template '{name}' is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return new PromptTemplate(name, reader.ReadToEnd().ReplaceLineEndings("\n").Trim());
    }

    // One pass over the template: a value that contains "{{question}}" (a document, a question) is never expanded.
    public string Render(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return Placeholder().Replace(Text, match => values.TryGetValue(match.Groups[1].Value, out var value)
            ? value
            : throw new InvalidOperationException($"The prompt template '{Name}' has no value for '{match.Groups[1].Value}'."));
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
}
