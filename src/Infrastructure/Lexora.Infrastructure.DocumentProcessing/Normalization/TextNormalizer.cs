using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexora.Infrastructure.DocumentProcessing.Normalization;

public static partial class TextNormalizer
{
    public static string Normalize(string text)
    {
        var cleaned = WithoutInvisibleCharacters(text.ReplaceLineEndings("\n").Normalize(NormalizationForm.FormC));
        var joined = LineBreakHyphenation().Replace(cleaned, "$1$2");
        var lines = joined.Split('\n').Select(line => RepeatedSpaces().Replace(line, " ").Trim());

        return BlankLineRuns().Replace(string.Join('\n', lines), "\n\n").Trim();
    }

    private static string WithoutInvisibleCharacters(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            if (character is '\n')
            {
                builder.Append(character);
            }
            else if (character is '\t' or ' ' || char.GetUnicodeCategory(character) is UnicodeCategory.SpaceSeparator)
            {
                builder.Append(' ');
            }
            else if (!char.IsControl(character) && char.GetUnicodeCategory(character) is not UnicodeCategory.Format)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"(\p{L})-\n(\p{Ll})")]
    private static partial Regex LineBreakHyphenation();

    [GeneratedRegex(" {2,}")]
    private static partial Regex RepeatedSpaces();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLineRuns();
}
