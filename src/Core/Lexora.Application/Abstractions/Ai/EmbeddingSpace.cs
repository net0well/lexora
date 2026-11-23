namespace Lexora.Application.Abstractions.Ai;

public sealed record EmbeddingSpace(string Provider, string Model, int Dimensions)
{
    public string Key => $"{Provider}:{Model}:{Dimensions}".ToLowerInvariant();
}
