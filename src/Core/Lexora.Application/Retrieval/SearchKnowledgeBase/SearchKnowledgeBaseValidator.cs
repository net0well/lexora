using FluentValidation;

namespace Lexora.Application.Retrieval.SearchKnowledgeBase;

public sealed class SearchKnowledgeBaseValidator : AbstractValidator<SearchKnowledgeBaseQuery>
{
    public const int MaxQueryLength = 1_000;
    public const int MaxTopK = 50;

    public SearchKnowledgeBaseValidator()
    {
        RuleFor(query => query.Query)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("search.query.required")
            .MaximumLength(MaxQueryLength).WithErrorCode("search.query.tooLong");
        RuleFor(query => query.Mode).IsInEnum().WithErrorCode("search.mode.invalid");
        RuleFor(query => query.TopK).InclusiveBetween(1, MaxTopK).WithErrorCode("search.topK.invalid");
    }
}
