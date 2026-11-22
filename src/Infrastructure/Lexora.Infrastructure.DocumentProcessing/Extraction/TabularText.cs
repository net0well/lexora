namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public static class TabularText
{
    public static string Lines(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows.Count == 0)
        {
            return string.Empty;
        }

        var header = rows[0];
        var lines = rows.Skip(1)
            .Select(row => string.Join("; ", row
                .Select((value, column) => (Header: HeaderOf(header, column), Value: value))
                .Where(cell => cell.Value.Length > 0)
                .Select(cell => $"{cell.Header}: {cell.Value}")))
            .Where(line => line.Length > 0);

        return string.Join('\n', lines);
    }

    private static string HeaderOf(IReadOnlyList<string> header, int column) =>
        column < header.Count && header[column].Length > 0 ? header[column] : $"Column {column + 1}";
}
