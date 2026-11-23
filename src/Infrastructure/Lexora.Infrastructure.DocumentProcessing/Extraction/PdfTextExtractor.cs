using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public sealed class PdfTextExtractor(IOptions<DocumentProcessingOptions> options) : IFormatTextExtractor
{
    public IReadOnlyCollection<DocumentFormat> Formats { get; } = [DocumentFormat.Pdf];

    public async Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken)
    {
        await using var seekable = await SeekableContent.FromAsync(content, cancellationToken);

        try
        {
            using var document = PdfDocument.Open(seekable.Stream);

            if (document.NumberOfPages > options.Value.MaxPages)
            {
                return Result.Failure<ExtractedDocument>(ExtractionFailures.PageLimit);
            }

            List<ExtractedSection> sections = [];

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                sections.Add(new ExtractedSection(null, page.Number, null, ContentOrderTextExtractor.GetText(page).Trim()));
            }

            return new ExtractedDocument(sections, document.NumberOfPages);
        }
        catch (PdfDocumentEncryptedException)
        {
            return Result.Failure<ExtractedDocument>(ExtractionFailures.Encrypted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // PdfPig reports broken files through many exception types; any of them means the file is unreadable.
            return Result.Failure<ExtractedDocument>(ExtractionFailures.Unreadable);
        }
    }
}
