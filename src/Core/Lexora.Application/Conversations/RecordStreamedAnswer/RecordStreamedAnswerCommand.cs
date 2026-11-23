using System.Globalization;
using System.Text;
using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.RecordStreamedAnswer;

public sealed record RecordStreamedAnswerCommand(
    Guid ConversationId,
    Guid UserId,
    string Question,
    DateTimeOffset AskedAt,
    string Answer,
    IReadOnlyList<Citation> Citations,
    TokenUsage Usage,
    string Provider,
    string Model,
    long LatencyMs)
{
    // Conversation content must never reach the logs, which print failed messages.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(CultureInfo.InvariantCulture, $"ConversationId = {ConversationId}, UserId = {UserId}, Question = <{Question.Length} chars>, ");
        builder.Append(CultureInfo.InvariantCulture, $"Answer = <{Answer.Length} chars>, Citations = {Citations.Count}, Provider = {Provider}, Model = {Model}");

        return true;
    }
}
