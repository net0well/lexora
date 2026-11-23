using System.Runtime.CompilerServices;
using System.Text;
using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Mapping;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Adapters;

public sealed class ChatModelAdapter(ChatClientRegistry registry, TimeProvider timeProvider) : IChatModel
{
    public async Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolved = registry.Resolve(request.Profile);
        var started = timeProvider.GetTimestamp();

        var response = await resolved.Client.GetResponseAsync(
            ChatMessageMapper.ToMessages(request),
            ChatMessageMapper.ToOptions(request, resolved.Profile),
            cancellationToken);

        return Completion(resolved, response.Text, response.ModelId, response.Usage, timeProvider.GetElapsedTime(started));
    }

    public async IAsyncEnumerable<ChatStreamUpdate> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resolved = registry.Resolve(request.Profile);
        var started = timeProvider.GetTimestamp();
        var text = new StringBuilder();
        string? modelId = null;
        UsageDetails? usage = null;

        await foreach (var update in resolved.Client.GetStreamingResponseAsync(
            ChatMessageMapper.ToMessages(request),
            ChatMessageMapper.ToOptions(request, resolved.Profile),
            cancellationToken))
        {
            modelId ??= update.ModelId;
            usage = update.Contents.OfType<UsageContent>().LastOrDefault()?.Details ?? usage;

            if (!string.IsNullOrEmpty(update.Text))
            {
                text.Append(update.Text);
                yield return ChatStreamUpdate.Delta(update.Text);
            }
        }

        yield return ChatStreamUpdate.Finished(Completion(resolved, text.ToString(), modelId, usage, timeProvider.GetElapsedTime(started)));
    }

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
