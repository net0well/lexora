using FluentValidation;
using Lexora.Domain.KnowledgeBases;

namespace Lexora.Application.KnowledgeBases;

public static class KnowledgeBaseRuleExtensions
{
    public const string InvalidChunkingKey = "knowledgeBase.chunking.invalid";

    public static IRuleBuilderOptions<T, string?> ValidKnowledgeBaseName<T>(this IRuleBuilderInitial<T, string?> rule) =>
        rule.Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("knowledgeBase.name.required")
            .MinimumLength(KnowledgeBaseName.MinLength).WithErrorCode("knowledgeBase.name.tooShort")
            .MaximumLength(KnowledgeBaseName.MaxLength).WithErrorCode("knowledgeBase.name.tooLong");

    public static IRuleBuilderOptions<T, string?> ValidKnowledgeBaseDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(KnowledgeBase.DescriptionMaxLength).WithErrorCode("knowledgeBase.description.tooLong");

    public static IRuleBuilderOptions<T, int?> ValidChunkMaxTokens<T>(this IRuleBuilder<T, int?> rule) =>
        rule.InclusiveBetween(ChunkingSettings.MinimumMaxTokens, ChunkingSettings.MaximumMaxTokens).WithErrorCode(InvalidChunkingKey);

    public static IRuleBuilderOptions<T, int?> ValidChunkOverlapTokens<T>(this IRuleBuilder<T, int?> rule) =>
        rule.GreaterThanOrEqualTo(0).WithErrorCode(InvalidChunkingKey);
}
