using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing.Detection;
using Lexora.Shared.Results;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public sealed class DocumentTextExtractor(IEnumerable<IFormatTextExtractor> extractors) : IDocumentTextExtractor
{
    private readonly Dictionary<DocumentFormat, IFormatTextExtractor> _extractors = extractors
        .SelectMany(extractor => extractor.Formats.Select(format => (format, extractor)))
        .ToDictionary(pair => pair.format, pair => pair.extractor);

    public Task<DocumentFormat?> DetectFormatAsync(Stream content, string fileName, CancellationToken cancellationToken) =>
        FileSignatureInspector.DetectAsync(content, fileName, cancellationToken);

    public async Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken)
    {
        if (!_extractors.TryGetValue(format, out var extractor))
        {
            return Result.Failure<ExtractedDocument>(ExtractionFailures.Unreadable);
        }

        var extracted = await extractor.ExtractAsync(content, format, cancellationToken);

        if (extracted.IsFailure)
        {
            return extracted;
        }

        return extracted.Value.Sections.Any(section => !string.IsNullOrWhiteSpace(section.Text))
            ? extracted
            : Result.Failure<ExtractedDocument>(ExtractionFailures.NoText);
    }
}
