using Lexora.Application.Abstractions.Ai;
using Lexora.Domain.Conversations;

namespace Lexora.Application.Conversations.Prompts;

public sealed class FollowUpQuestionRewriter(IChatModel chat) : IFollowUpQuestionRewriter
{
    public const int MaxOutputTokens = 128;

    private static readonly PromptTemplate _system = PromptTemplate.Load("rewrite-system");
    private static readonly PromptTemplate _user = PromptTemplate.Load("rewrite-user");
    private static readonly char[] _quotes = ['"', '\'', '“', '”', '«', '»'];

    public static string SystemPrompt => _system.Text;

    public async Task<string> RewriteAsync(string question, IReadOnlyList<ConversationMessage> history, string? profile, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count == 0)
        {
            return question;
        }

        // The conversation goes as a transcript in one turn: as real chat turns, small models tend to answer the question instead.
        var prompt = _user.Render(new Dictionary<string, string>
        {
            ["conversation"] = string.Join('\n', history.Select(message => $"{Speaker(message.Role)}: {PromptText.Neutralize(message.Content)}")),
            ["question"] = PromptText.Neutralize(question.Trim()),
        });

        var completion = await chat.CompleteAsync(
            new ChatRequest(profile, SystemPrompt, [new ChatTurn(ChatTurnRole.User, prompt)], Temperature: 0, MaxOutputTokens: MaxOutputTokens),
            cancellationToken);
        var rewritten = completion.Text.Trim().Trim(_quotes).Trim();

        // An empty or rambling reply (a reasoning model can spend the whole budget thinking) is worse than the original question.
        return rewritten.Length is 0 or > Conversation.MaxQuestionLength || rewritten.Contains('\n', StringComparison.Ordinal) ? question : rewritten;
    }

    private static string Speaker(MessageRole role) => role == MessageRole.User ? "User" : "Assistant";
}
