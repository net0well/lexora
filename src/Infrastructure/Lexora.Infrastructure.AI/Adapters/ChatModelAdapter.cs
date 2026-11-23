using System.Runtime.CompilerServices;
using System.Text;
using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Mapping;
using Lexora.Infrastructure.AI.Registry;
using Lexora.Infrastructure.AI.Resilience;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Adapters;

public sealed class ChatModelAdapter(ChatClientRegistry registry, AiResiliencePipelineProvider pipelines, TimeProvider timeProvider) : IChatModel
{
    public async Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolved = registry.Resolve(request.Profile);
        var messages = ChatMessageMapper.ToMessages(request);
        var options = ChatMessageMapper.ToOptions(request, resolved.Profile);
        var started = timeProvider.GetTimestamp();

        var response = await ExecuteAsync(
            resolved,
            async token => await resolved.Client.GetResponseAsync(messages, options, token),
            cancellationToken);

        return Completion(resolved, response.Text, response.ModelId, response.Usage, timeProvider.GetElapsedTime(started));
    }

    public async IAsyncEnumerable<ChatStreamUpdate> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolved = registry.Resolve(request.Profile);
        var messages = ChatMessageMapper.ToMessages(request);
        var options = ChatMessageMapper.ToOptions(request, resolved.Profile);
        var started = timeProvider.GetTimestamp();

        // Only opening the stream is retried: once text reached the caller, a retry would repeat it.
        var stream = await ExecuteAsync(resolved, token => OpenAsync(resolved.Client, messages, options, token), cancellationToken);
        await using var updates = stream.Updates;

        var text = new StringBuilder();
        string? modelId = null;
        UsageDetails? usage = null;
        var hasCurrent = stream.HasCurrent;

        while (hasCurrent)
        {
            var update = updates.Current;
            modelId ??= update.ModelId;
            usage = update.Contents.OfType<UsageContent>().LastOrDefault()?.Details ?? usage;

            if (!string.IsNullOrEmpty(update.Text))
            {
                text.Append(update.Text);
                yield return ChatStreamUpdate.Delta(update.Text);
            }

            hasCurrent = await NextAsync(resolved, updates, cancellationToken);
        }

        yield return ChatStreamUpdate.Finished(Completion(resolved, text.ToString(), modelId, usage, timeProvider.GetElapsedTime(started)));
    }

    private async Task<T> ExecuteAsync<T>(ChatProfileClient resolved, Func<CancellationToken, ValueTask<T>> call, CancellationToken cancellationToken)
    {
        try
        {
            return await pipelines.ForChat(resolved.ProfileName).ExecuteAsync(call, cancellationToken);
        }
        catch (Exception exception) when (AiFailureClassifier.IsUnavailable(exception, cancellationToken))
        {
            throw Unavailable(resolved, exception);
        }
    }

    private static async ValueTask<ChatStreamStart> OpenAsync(IChatClient client, List<ChatMessage> messages, ChatOptions options, CancellationToken cancellationToken)
    {
        var updates = client.GetStreamingResponseAsync(messages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);

        try
        {
            return new ChatStreamStart(updates, await updates.MoveNextAsync());
        }
        catch
        {
            await updates.DisposeAsync();
            throw;
        }
    }

    private static async ValueTask<bool> NextAsync(ChatProfileClient resolved, IAsyncEnumerator<ChatResponseUpdate> updates, CancellationToken cancellationToken)
    {
        try
        {
            return await updates.MoveNextAsync();
        }
        catch (Exception exception) when (AiFailureClassifier.IsUnavailable(exception, cancellationToken))
        {
            throw Unavailable(resolved, exception);
        }
    }

    private static AiProviderUnavailableException Unavailable(ChatProfileClient resolved, Exception exception) =>
        new(ChatMessageMapper.ProviderNameOf(resolved.Profile.Provider), resolved.ProfileName, AiFailureClassifier.RetryAfterOf(exception), exception);

    private static ChatCompletion Completion(ChatProfileClient resolved, string text, string? modelId, UsageDetails? usage, TimeSpan latency) =>
        new(
            text,
            ChatMessageMapper.ProviderNameOf(resolved.Profile.Provider),
            modelId ?? resolved.Profile.Model,
            TokensOf(usage?.InputTokenCount),
            TokensOf(usage?.OutputTokenCount),
            latency);

    private static int? TokensOf(long? count) => count is { } value ? (int)Math.Min(value, int.MaxValue) : null;
}
