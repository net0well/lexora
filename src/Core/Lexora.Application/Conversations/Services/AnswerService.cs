using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Ai;
using Lexora.Application.Common.Options;
using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.Mappers;
using Lexora.Application.Conversations.Prompts;
using Lexora.Application.Retrieval;
using Lexora.Application.Retrieval.Services;
using Lexora.Domain.Conversations;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Conversations.Services;

public sealed class AnswerService(
    IConversationRepository conversations,
    ICurrentUser currentUser,
    IRetrievalService retrieval,
    IRagPromptBuilder prompts,
    IChatModel chat,
    ICitationParser citations,
    IAiProfileCatalog profiles,
    IOptions<RagOptions> options,
    TimeProvider timeProvider) : IAnswerService
{
    public const string NoContextProvider = "lexora";
    public const string NoContextModel = "no-context";

    public async Task<Result<AskQuestionResponse>> AnswerAsync(AskQuestionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var conversation = await conversations.GetByIdAsync(command.ConversationId, cancellationToken);

        // A conversation of another user answers like a missing one, so ids cannot be probed.
        if (conversation is null || !conversation.IsOwnedBy(currentUser.UserId))
        {
            return Result.Failure<AskQuestionResponse>(ConversationNotifications.NotFound);
        }

        if (command.Profile is { } profile && !profiles.IsSelectable(profile))
        {
            return Result.Failure<AskQuestionResponse>(AiNotifications.ProfileNotSelectable);
        }

        var settings = options.Value;
        var history = conversation.RecentHistory(settings.HistoryMessages);
        var asked = conversation.AddUserMessage(command.Question, timeProvider.GetUtcNow());

        if (asked.IsFailure)
        {
            return Result.Failure<AskQuestionResponse>(asked.Notifications);
        }

        var question = asked.Value.Content;
        var hits = await retrieval.RetrieveAsync(conversation.KnowledgeBaseId, question, SearchMode.Hybrid, settings.TopK, cancellationToken);

        if (hits.Count == 0)
        {
            return Record(conversation, NoContextAnswer.For(question), [], TokenUsage.None, NoContextProvider, NoContextModel, latencyMs: 0);
        }

        var prompt = prompts.Build(question, hits, history);
        var completion = await chat.CompleteAsync(new ChatRequest(command.Profile, prompt.SystemPrompt, prompt.Turns), cancellationToken);
        var answer = string.IsNullOrWhiteSpace(completion.Text) ? NoContextAnswer.For(question) : completion.Text.Trim();

        return Record(
            conversation,
            answer,
            citations.Parse(answer, prompt.Excerpts),
            TokenUsage.Create(completion.InputTokens ?? 0, completion.OutputTokens ?? 0),
            completion.Provider,
            completion.Model,
            (long)completion.Latency.TotalMilliseconds);
    }

    private Result<AskQuestionResponse> Record(
        Conversation conversation,
        string answer,
        IReadOnlyList<Citation> cited,
        TokenUsage usage,
        string provider,
        string model,
        long latencyMs)
    {
        var recorded = conversation.AddAssistantMessage(answer, cited, usage, provider, model, latencyMs, timeProvider.GetUtcNow());

        if (recorded.IsFailure)
        {
            return Result.Failure<AskQuestionResponse>(recorded.Notifications);
        }

        return new AskQuestionResponse(
            conversation.Id,
            recorded.Value.Id,
            answer,
            [.. cited.Select(ConversationMapper.ToResponse)],
            Grounded: cited.Count > 0,
            provider,
            model,
            usage.InputTokens,
            usage.OutputTokens,
            latencyMs);
    }
}
