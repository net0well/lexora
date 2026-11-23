using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class StubChatClient : IChatClient
{
    public List<(List<ChatMessage> Messages, ChatOptions? Options)> Calls { get; } = [];

    public ChatResponse Response { get; set; } = new(new ChatMessage(ChatRole.Assistant, "answer"));

    public IReadOnlyList<ChatResponseUpdate> Updates { get; set; } = [];

    public Queue<Exception> Failures { get; } = new();

    public bool Disposed { get; private set; }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls.Add(([.. messages], options));

        return Failures.TryDequeue(out var failure) ? Task.FromException<ChatResponse>(failure) : Task.FromResult(Response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Calls.Add(([.. messages], options));

        if (Failures.TryDequeue(out var failure))
        {
            throw failure;
        }

        foreach (var update in Updates)
        {
            await Task.Yield();
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() => Disposed = true;
}
