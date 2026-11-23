using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers.Fake;

public sealed class FakeChatClient(string model) : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var history = messages.ToList();
        var answer = AnswerTo(history);

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer))
        {
            ModelId = model,
            FinishReason = ChatFinishReason.Stop,
            Usage = UsageOf(history, answer),
        });
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = messages.ToList();
        var answer = AnswerTo(history);
        var words = answer.Split(' ');

        for (var index = 0; index < words.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return new ChatResponseUpdate(ChatRole.Assistant, index == 0 ? words[index] : " " + words[index]) { ModelId = model };
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(UsageOf(history, answer))])
        {
            ModelId = model,
            FinishReason = ChatFinishReason.Stop,
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        return serviceKey is not null ? null
            : serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("fake", null, model)
            : serviceType.IsInstanceOfType(this) ? this
            : null;
    }

    public void Dispose()
    {
    }

    private static string AnswerTo(List<ChatMessage> history)
    {
        var question = history.LastOrDefault(message => message.Role == ChatRole.User)?.Text.Trim() ?? string.Empty;

        return $"This is a simulated answer to \"{question}\" based on the excerpts [1] and [2].";
    }

    private static UsageDetails UsageOf(List<ChatMessage> history, string answer)
    {
        var input = history.Sum(message => WordsIn(message.Text));
        var output = WordsIn(answer);

        return new UsageDetails { InputTokenCount = input, OutputTokenCount = output, TotalTokenCount = input + output };
    }

    private static int WordsIn(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
