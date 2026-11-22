using Lexora.Application.Documents.DeleteDocument;
using Lexora.Application.Documents.ReprocessDocument;
using Lexora.Application.Documents.UploadDocument;
using Lexora.Shared.Results;

namespace Lexora.Application.Documents.Services;

public interface IDocumentService
{
    Task<Result<UploadDocumentResponse>> UploadAsync(UploadDocumentCommand command, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(DeleteDocumentCommand command, CancellationToken cancellationToken);

    Task<Result<ReprocessDocumentResponse>> ReprocessAsync(ReprocessDocumentCommand command, CancellationToken cancellationToken);
}
