using FluentValidation;
using Lexora.Application.Common.Validation;

namespace Lexora.Application.Documents.ListDocumentChunks;

public sealed class ListDocumentChunksValidator : AbstractValidator<ListDocumentChunksQuery>
{
    public ListDocumentChunksValidator() => this.ValidPage(query => query.Page, query => query.PageSize);
}
