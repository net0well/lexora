using FluentValidation;

namespace Lexora.Application.KnowledgeBases.CreateKnowledgeBase;

public sealed class CreateKnowledgeBaseValidator : AbstractValidator<CreateKnowledgeBaseCommand>
{
    public CreateKnowledgeBaseValidator()
    {
        RuleFor(command => command.Name).ValidKnowledgeBaseName();
        RuleFor(command => command.Description).ValidKnowledgeBaseDescription();
        RuleFor(command => command.ChunkMaxTokens).ValidChunkMaxTokens();
        RuleFor(command => command.ChunkOverlapTokens).ValidChunkOverlapTokens();
    }
}
