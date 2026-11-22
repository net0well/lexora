using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Shared.Results;

namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public interface IFormatTextExtractor
{
    IReadOnlyCollection<DocumentFormat> Formats { get; }

    Task<Result<ExtractedDocument>> ExtractAsync(Stream content, DocumentFormat format, CancellationToken cancellationToken);
}
