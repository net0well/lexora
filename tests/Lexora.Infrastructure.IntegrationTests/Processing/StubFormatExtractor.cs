using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing.Extraction;
using Lexora.Shared.Results;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

internal sealed class StubFormatExtractor(DocumentFormat format, Result<ExtractedDocument> result) : IFormatTextExtractor
{
    public IReadOnlyCollection<DocumentFormat> Formats { get; } = [format];

    public Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken) =>
        Task.FromResult(result);
}
