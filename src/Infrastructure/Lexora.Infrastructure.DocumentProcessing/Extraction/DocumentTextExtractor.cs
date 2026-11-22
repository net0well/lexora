using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing.Detection;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public sealed class DocumentTextExtractor : IDocumentTextExtractor
{
    public Task<DocumentFormat?> DetectFormatAsync(Stream content, string fileName, CancellationToken cancellationToken) =>
        FileSignatureInspector.DetectAsync(content, fileName, cancellationToken);
}
