using System.Globalization;
using System.Text;

namespace Lexora.Application.Conversations.AskQuestion;

public sealed record AskQuestionCommand(Guid ConversationId, string Question, string? Profile = null)
{
    // Conversation content must never reach the logs, which print failed messages.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(CultureInfo.InvariantCulture, $"ConversationId = {ConversationId}, Question = <{Question.Length} chars>, Profile = {Profile}");

        return true;
    }
}
