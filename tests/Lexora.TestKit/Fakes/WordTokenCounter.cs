using Lexora.Application.Abstractions.Processing;

namespace Lexora.TestKit.Fakes;

public sealed class WordTokenCounter : ITokenCounter
{
    public int Count(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
