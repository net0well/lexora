using System.Runtime.CompilerServices;
using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Ai;
using Lexora.Application.Common.Options;
using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.Mappers;
using Lexora.Application.Conversations.Prompts;
using Lexora.Application.Conversations.RecordStreamedAnswer;
using Lexora.Application.Conversations.Streaming;
using Lexora.Domain.Conversations;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Lexora.Application.Conversations.Services;

public sealed class AnswerStreamingService(
    IConversationRepository conversations,
    ICurrentUser currentUser,
    IAnswerContextBuilder contexts,
    IChatModel chat,
    ICitationParser citations,
    IAiProfileCatalog profiles,
    IMessageBus bus,
    IOptions<RagOptions> options,
    TimeProvider timeProvider) : IAnswerStreamingService
{
    private static readonly AskQuestionValidator _validator = new();

    public async IAsyncEnumerable<AnswerStreamEvent> StreamAsync(AskQuestionCommand command, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Streaming bypasses the message pipeline, so the validation it would run happens here.
        var validation = await _validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            yield return new AnswerErrorEvent([.. validation.Errors.Select(error => Notification.Validation(error.ErrorCode, error.ErrorMessage))]);
            yield break;
        }

        var conversation = await conversations.GetByIdAsync(command.ConversationId, cancellationToken);

        if (conversation is null || !conversation.IsOwnedBy(currentUser.UserId))
        {
            yield return new AnswerErrorEvent([ConversationNotifications.NotFound]);
            yield break;
        }

        if (command.Profile is { } profile && !profiles.IsSelectable(profile))
        {
            yield return new AnswerErrorEvent([AiNotifications.ProfileNotSelectable]);
            yield break;
        }

        var askedAt = timeProvider.GetUtcNow();
        var question = command.Question.Trim();
        var context = await contexts.BuildAsync(
            conversation.KnowledgeBaseId,
            question,
            conversation.RecentHistory(options.Value.HistoryMessages),
            command.Profile,
            cancellationToken);

        var answer = NoContextAnswer.For(question);
        IReadOnlyList<Citation> cited = [];
        var usage = TokenUsage.None;
        var (provider, model, latencyMs) = (AnswerService.NoContextProvider, AnswerService.NoContextModel, 0L);

        if (context.Prompt is { } prompt)
        {
            ChatCompletion? completed = null;

            await foreach (var update in chat.StreamAsync(new ChatRequest(command.Profile, prompt.SystemPrompt, prompt.Turns), cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.TextDelta))
                {
                    yield return new AnswerDeltaEvent(update.TextDelta);
                }

                completed = update.Completed ?? completed;
            }

            if (completed is null)
            {
                throw new InvalidOperationException("The chat model stream ended without a completion.");
            }

            if (!string.IsNullOrWhiteSpace(completed.Text))
            {
                answer = completed.Text.Trim();
                cited = citations.Parse(answer, prompt.Excerpts);
            }
            else
            {
                yield return new AnswerDeltaEvent(answer);
            }

            usage = TokenUsage.Create(completed.InputTokens ?? 0, completed.OutputTokens ?? 0);
            (provider, model, latencyMs) = (completed.Provider, completed.Model, (long)completed.Latency.TotalMilliseconds);
        }
        else
        {
            yield return new AnswerDeltaEvent(answer);
        }

        yield return new AnswerCitationsEvent([.. cited.Select(ConversationMapper.ToResponse)], Grounded: cited.Count > 0);

        var recorded = await bus.InvokeAsync<Result<RecordStreamedAnswerResponse>>(
            new RecordStreamedAnswerCommand(conversation.Id, currentUser.UserId, question, askedAt, answer, cited, usage, provider, model, latencyMs),
            cancellationToken);

        if (recorded.IsFailure)
        {
            yield return new AnswerErrorEvent(recorded.Notifications);
            yield break;
        }

        yield return new AnswerDoneEvent(conversation.Id, recorded.Value.MessageId, provider, model, usage.InputTokens, usage.OutputTokens, latencyMs);
    }
}
