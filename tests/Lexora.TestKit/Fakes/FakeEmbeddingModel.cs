using Lexora.Application.Abstractions.Ai;

namespace Lexora.TestKit.Fakes;

public sealed class FakeEmbeddingModel(int dimensions = 4) : IEmbeddingModel
{
    private readonly List<(IReadOnlyList<string> Inputs, EmbeddingPurpose Purpose)> _calls = [];

    public EmbeddingSpace Space { get; } = new("fake", "fake-embedding", dimensions);

    public IReadOnlyList<(IReadOnlyList<string> Inputs, EmbeddingPurpose Purpose)> Calls => _calls;

    public Exception? FailWith { get; set; }

    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, EmbeddingPurpose purpose, CancellationToken cancellationToken)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        _calls.Add(([.. inputs], purpose));

        return Task.FromResult<IReadOnlyList<float[]>>([.. inputs.Select(input => Enumerable.Repeat((float)input.Length, Space.Dimensions).ToArray())]);
    }
}
