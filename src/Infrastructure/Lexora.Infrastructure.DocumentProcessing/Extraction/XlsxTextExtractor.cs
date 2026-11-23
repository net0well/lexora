using System.Globalization;
using ClosedXML.Excel;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public sealed class XlsxTextExtractor(IOptions<DocumentProcessingOptions> options) : IFormatTextExtractor
{
    public IReadOnlyCollection<DocumentFormat> Formats { get; } = [DocumentFormat.Xlsx];

    public async Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken)
    {
        await using var seekable = await SeekableContent.FromAsync(content, cancellationToken);

        try
        {
            using var workbook = new XLWorkbook(seekable.Stream);
            var ranges = workbook.Worksheets
                .Select(sheet => (Sheet: sheet, Range: sheet.RangeUsed()))
                .ToList();

            if (ranges.Sum(pair => pair.Range is null ? 0L : (long)pair.Range.RowCount() * pair.Range.ColumnCount()) > options.Value.MaxCells)
            {
                return Result.Failure<ExtractedDocument>(ExtractionFailures.CellLimit);
            }

            List<ExtractedSection> sections = [];

            foreach (var (sheet, range) in ranges)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (range is null)
                {
                    continue;
                }

                var rows = range.Rows()
                    .Select(row => (IReadOnlyList<string>)[.. row.Cells().Select(ValueOf)])
                    .Where(row => row.Any(value => value.Length > 0))
                    .ToList();
                var text = TabularText.Lines(rows);

                if (text.Length > 0)
                {
                    sections.Add(new ExtractedSection(null, null, sheet.Name, text));
                }
            }

            return new ExtractedDocument(sections, Math.Max(1, workbook.Worksheets.Count));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // ClosedXML reports damaged workbooks through several exception types; all mean an unreadable file.
            return Result.Failure<ExtractedDocument>(ExtractionFailures.Unreadable);
        }
    }

    private static string ValueOf(IXLCell cell) =>
        (cell.HasFormula ? cell.CachedValue.ToString(CultureInfo.InvariantCulture) : cell.GetFormattedString()).Trim();
}
