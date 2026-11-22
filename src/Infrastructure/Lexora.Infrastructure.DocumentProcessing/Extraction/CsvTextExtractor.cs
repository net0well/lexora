using System.Text;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public sealed class CsvTextExtractor(IOptions<DocumentProcessingOptions> options) : IFormatTextExtractor
{
    private static readonly char[] _delimiters = [',', ';', '\t'];

    public IReadOnlyCollection<DocumentFormat> Formats { get; } = [DocumentFormat.Csv];

    public async Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken)
    {
        var text = await TextReading.ReadAllAsync(content, cancellationToken);
        var delimiter = DetectDelimiter(text);
        var rows = Parse(text, delimiter).Where(row => row.Any(cell => cell.Length > 0)).ToList();

        if (rows.Sum(row => row.Count) > options.Value.MaxCells)
        {
            return Result.Failure<ExtractedDocument>(ExtractionFailures.CellLimit);
        }

        var lines = TabularText.Lines(rows);

        return new ExtractedDocument(lines.Length == 0 ? [] : [new ExtractedSection(null, null, null, lines)], PageCount: 1);
    }

    public static char DetectDelimiter(string text)
    {
        var firstLine = text.Split('\n').FirstOrDefault(line => line.Trim().Length > 0) ?? string.Empty;

        return _delimiters.OrderByDescending(candidate => CountOutsideQuotes(firstLine, candidate)).First();
    }

    public static IEnumerable<List<string>> Parse(string text, char delimiter)
    {
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];

            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    cell.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(character);
                }

                continue;
            }

            if (character == '"')
            {
                quoted = true;
            }
            else if (character == delimiter)
            {
                row.Add(cell.ToString().Trim());
                cell.Clear();
            }
            else if (character == '\n')
            {
                row.Add(cell.ToString().Trim());
                cell.Clear();
                yield return row;
                row = [];
            }
            else
            {
                cell.Append(character);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString().Trim());
            yield return row;
        }
    }

    private static int CountOutsideQuotes(string line, char candidate)
    {
        var count = 0;
        var quoted = false;

        foreach (var character in line)
        {
            if (character == '"')
            {
                quoted = !quoted;
            }
            else if (character == candidate && !quoted)
            {
                count++;
            }
        }

        return count;
    }
}
