using System.Text;

namespace Lexora.Infrastructure.Persistence.Queries;

public static class LikePattern
{
    public const char Escape = '\\';
    public const string EscapeCharacter = @"\";

    public static string Contains(string value)
    {
        var pattern = new StringBuilder(value.Length + 2).Append('%');

        foreach (var character in value)
        {
            if (character is '%' or '_' or Escape)
            {
                pattern.Append(Escape);
            }

            pattern.Append(character);
        }

        return pattern.Append('%').ToString();
    }
}
