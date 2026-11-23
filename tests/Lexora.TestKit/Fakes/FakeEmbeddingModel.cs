using System.Collections.Concurrent;
using Lexora.Application.Abstractions.Ai;

namespace Lexora.TestKit.Fakes;

public sealed class FakeEmbeddingModel(int dimensions = 4) : IEmbeddingModel
{
    private readonly ConcurrentQueue<(IReadOnlyList<string> Inputs, EmbeddingPurpose Purpose)> _calls = new();
    private readonly ConcurrentQueue<Exception> _nextFailures = new();

    public EmbeddingSpace Space { get; } = new("fake", "fake-embedding", dimensions);

    public IReadOnlyList<(IReadOnlyList<string> Inputs, EmbeddingPurpose Purpose)> Calls => [.. _calls];

    public Exception? FailWith { get; set; }

    public int PendingFailures => _nextFailures.Count;

    public void FailNext(Exception exception, int times = 1)
    {
        for (var attempt = 0; attempt < times; attempt++)
        {
            _nextFailures.Enqueue(exception);
        }
    }

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, EmbeddingPurpose purpose, CancellationToken cancellationToken)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        if (_nextFailures.TryDequeue(out var failure))
        {
            throw failure;
        }

        _calls.Enqueue(([.. inputs], purpose));

        return Task.FromResult<IReadOnlyList<float[]>>([.. inputs.Select(input => Enumerable.Repeat((float)input.Length, Space.Dimensions).ToArray())]);
    }
}
