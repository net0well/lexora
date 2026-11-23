using System.Runtime.CompilerServices;
using Lexora.Application.Abstractions.Ai;

namespace Lexora.TestKit.Fakes;

public sealed class FakeChatModel : IChatModel
{
    public static readonly TimeSpan Latency = TimeSpan.FromMilliseconds(250);

    public List<ChatRequest> Requests { get; } = [];

    public Queue<string> Answers { get; } = new();

    public string DefaultAnswer { get; set; } = "Isole a área num raio de 50 metros [1].";

    public Exception? FailWith { get; set; }

    public Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        return FailWith is not null ? Task.FromException<ChatCompletion>(FailWith) : Task.FromResult(Completion(NextAnswer()));
    }

    public async IAsyncEnumerable<ChatStreamUpdate> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (FailWith is not null)
        {
            throw FailWith;
        }

        var answer = NextAnswer();
        var words = answer.Split(' ');

        for (var index = 0; index < words.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return ChatStreamUpdate.Delta(index == 0 ? words[index] : " " + words[index]);
        }

        yield return ChatStreamUpdate.Finished(Completion(answer));
    }

    private string NextAnswer() => Answers.TryDequeue(out var answer) ? answer : DefaultAnswer;

    private static ChatCompletion Completion(string answer) => new(answer, "fake", "fake-chat", 120, 12, Latency);
}
