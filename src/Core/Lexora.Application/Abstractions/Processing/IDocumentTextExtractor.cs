using Lexora.Domain.Documents;
using Lexora.Shared.Results;

namespace Lexora.Application.Abstractions.Processing;

public interface IDocumentTextExtractor
{
    Task<DocumentFormat?> DetectFormatAsync(Stream content, string fileName, CancellationToken cancellationToken);

    Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken);
}
