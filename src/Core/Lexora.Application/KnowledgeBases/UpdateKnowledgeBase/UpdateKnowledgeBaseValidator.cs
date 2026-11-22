using FluentValidation;

namespace Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;

public sealed class UpdateKnowledgeBaseValidator : AbstractValidator<UpdateKnowledgeBaseCommand>
{
    public UpdateKnowledgeBaseValidator()
    {
        RuleFor(command => command.Name).ValidKnowledgeBaseName();
        RuleFor(command => command.Description).ValidKnowledgeBaseDescription();
        RuleFor(command => command.ChunkMaxTokens).ValidChunkMaxTokens();
        RuleFor(command => command.ChunkOverlapTokens).ValidChunkOverlapTokens();
    }
}
