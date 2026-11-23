using Lexora.Application.Abstractions.Ai;
using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.Prompts;

public sealed class FollowUpQuestionRewriter(IChatModel chat) : IFollowUpQuestionRewriter
{
    public const int MaxOutputTokens = 128;

    public const string SystemPrompt =
        "Rewrite the last question of the user as a standalone question that can be understood without the conversation. " +
        "Keep the language of the question and every name, code and number it refers to. Reply with the rewritten question only.";

    private static readonly char[] _quotes = ['"', '\'', '“', '”', '«', '»'];

    public async Task<string> RewriteAsync(string question, IReadOnlyList<ConversationMessage> history, string? profile, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count == 0)
        {
            return question;
        }

        List<ChatTurn> turns =
        [
            .. history.Select(message => new ChatTurn(message.Role == MessageRole.User ? ChatTurnRole.User : ChatTurnRole.Assistant, message.Content)),
            new ChatTurn(ChatTurnRole.User, $"Question to rewrite: {question}"),
        ];

        var completion = await chat.CompleteAsync(new ChatRequest(profile, SystemPrompt, turns, Temperature: 0, MaxOutputTokens: MaxOutputTokens), cancellationToken);
        var rewritten = completion.Text.Trim().Trim(_quotes).Trim();

        // An empty or rambling reply (a reasoning model can spend the whole budget thinking) is worse than the original question.
        return rewritten.Length is 0 or > Conversation.MaxQuestionLength || rewritten.Contains('\n', StringComparison.Ordinal) ? question : rewritten;
    }
}
