using FluentValidation;
using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.StartConversation;

public sealed class StartConversationValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationValidator()
    {
        RuleFor(command => command.KnowledgeBaseId).NotEmpty().WithErrorCode("conversation.knowledgeBase.required");
        RuleFor(command => command.Title).MaximumLength(Conversation.MaxTitleLength).WithErrorCode("conversation.title.tooLong");
    }
}
