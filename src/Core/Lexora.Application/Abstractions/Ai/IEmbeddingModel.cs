namespace Lexora.Application.Abstractions.Ai;

public interface IEmbeddingModel
{
    EmbeddingSpace Space { get; }

    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, EmbeddingPurpose purpose, CancellationToken cancellationToken);
}
