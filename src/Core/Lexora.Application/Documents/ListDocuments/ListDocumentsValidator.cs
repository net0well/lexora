using FluentValidation;
using Lexora.Application.Common.Validation;
using Lexora.Domain.Documents;

namespace Lexora.Application.Documents.ListDocuments;

public sealed class ListDocumentsValidator : AbstractValidator<ListDocumentsQuery>
{
    public const int MaxSearchLength = 100;

    public ListDocumentsValidator()
    {
        this.ValidPage(query => query.Page, query => query.PageSize);

        RuleFor(query => query.Status)
            .Must(status => status is null || (Enum.IsDefined(status.Value) && status != DocumentStatus.Deleted))
            .WithErrorCode("document.status.invalid");
        RuleFor(query => query.Search).MaximumLength(MaxSearchLength).WithErrorCode("document.search.tooLong");
    }
}
