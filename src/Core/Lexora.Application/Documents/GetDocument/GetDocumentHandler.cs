using Lexora.Application.Documents.Abstractions;
using Lexora.Shared.Results;

namespace Lexora.Application.Documents.GetDocument;

public static class GetDocumentHandler
{
    public static async Task<Result<GetDocumentResponse>> Handle(GetDocumentQuery query, IDocumentQueries queries, CancellationToken cancellationToken)
    {
        var document = await queries.GetByIdAsync(query.Id, cancellationToken);

        return document is null ? Result.Failure<GetDocumentResponse>(DocumentNotifications.NotFound) : document;
    }
}
