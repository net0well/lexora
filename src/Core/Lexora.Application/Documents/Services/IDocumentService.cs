using Lexora.Application.Documents.UploadDocument;
using Lexora.Shared.Results;

namespace Lexora.Application.Documents.Services;

public interface IDocumentService
{
    Task<Result<UploadDocumentResponse>> UploadAsync(UploadDocumentCommand command, CancellationToken cancellationToken);
}
