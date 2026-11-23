using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Ai;
using Lexora.Application.Common.Options;
using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.Mappers;
using Lexora.Application.Conversations.Prompts;
using Lexora.Application.Conversations.RecordStreamedAnswer;
using Lexora.Domain.Conversations;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Conversations.Services;

public sealed class AnswerService(
    IConversationRepository conversations,
    ICurrentUser currentUser,
    IAnswerContextBuilder contexts,
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

        var history = conversation.RecentHistory(options.Value.HistoryMessages);
        var asked = conversation.AddUserMessage(command.Question, timeProvider.GetUtcNow());

        if (asked.IsFailure)
        {
            return Result.Failure<AskQuestionResponse>(asked.Notifications);
        }

        var context = await contexts.BuildAsync(conversation.KnowledgeBaseId, asked.Value.Content, history, command.Profile, cancellationToken);

        if (context.Prompt is not { } prompt)
        {
            return Record(conversation, NoContextAnswer.For(context.Question), [], TokenUsage.None, NoContextProvider, NoContextModel, latencyMs: 0);
        }

        var completion = await chat.CompleteAsync(new ChatRequest(command.Profile, prompt.SystemPrompt, prompt.Turns), cancellationToken);
        var answer = string.IsNullOrWhiteSpace(completion.Text) ? NoContextAnswer.For(context.Question) : completion.Text.Trim();

        return Record(
            conversation,
            answer,
            citations.Parse(answer, prompt.Excerpts),
            TokenUsage.Create(completion.InputTokens ?? 0, completion.OutputTokens ?? 0),
            completion.Provider,
            completion.Model,
            (long)completion.Latency.TotalMilliseconds);
    }

    public async Task<Result<RecordStreamedAnswerResponse>> RecordAsync(RecordStreamedAnswerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var conversation = await conversations.GetByIdAsync(command.ConversationId, cancellationToken);

        if (conversation is null || !conversation.IsOwnedBy(command.UserId))
        {
            return Result.Failure<RecordStreamedAnswerResponse>(ConversationNotifications.NotFound);
        }

        var asked = conversation.AddUserMessage(command.Question, command.AskedAt);

        if (asked.IsFailure)
        {
            return Result.Failure<RecordStreamedAnswerResponse>(asked.Notifications);
        }

        var answered = conversation.AddAssistantMessage(
            command.Answer,
            command.Citations,
            command.Usage,
            command.Provider,
            command.Model,
            command.LatencyMs,
            timeProvider.GetUtcNow());

        return answered.IsFailure
            ? Result.Failure<RecordStreamedAnswerResponse>(answered.Notifications)
            : new RecordStreamedAnswerResponse(answered.Value.Id);
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
