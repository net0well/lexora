using Lexora.Domain.Documents;

namespace Lexora.Application.Abstractions.Processing;

public interface IDocumentTextExtractor
{
    Task<DocumentFormat?> DetectFormatAsync(Stream content, string fileName, CancellationToken cancellationToken);
}
