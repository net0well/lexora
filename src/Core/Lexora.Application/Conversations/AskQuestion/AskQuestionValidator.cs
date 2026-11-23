using FluentValidation;
using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.AskQuestion;

public sealed class AskQuestionValidator : AbstractValidator<AskQuestionCommand>
{
    public const int MaxProfileLength = 64;

    public AskQuestionValidator()
    {
        RuleFor(command => command.Question)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("conversation.message.required")
            .MaximumLength(Conversation.MaxQuestionLength).WithErrorCode("conversation.message.tooLong");
        RuleFor(command => command.Profile).MaximumLength(MaxProfileLength).WithErrorCode("ai.profile.notSelectable");
    }
}
