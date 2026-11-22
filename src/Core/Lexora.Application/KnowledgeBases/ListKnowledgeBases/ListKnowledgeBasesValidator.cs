using FluentValidation;
using Lexora.Application.Common.Validation;

namespace Lexora.Application.KnowledgeBases.ListKnowledgeBases;

public sealed class ListKnowledgeBasesValidator : AbstractValidator<ListKnowledgeBasesQuery>
{
    public ListKnowledgeBasesValidator()
    {
        this.ValidPage(query => query.Page, query => query.PageSize);

        RuleFor(query => query.Status).IsInEnum().WithErrorCode("knowledgeBase.status.invalid");
    }
}
