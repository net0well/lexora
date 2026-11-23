using System.Text;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public static class TextReading
{
    public static async Task<string> ReadAllAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        return (await reader.ReadToEndAsync(cancellationToken)).ReplaceLineEndings("\n");
    }

    public static IEnumerable<string> Paragraphs(string text) =>
        text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
