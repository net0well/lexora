using Lexora.Application.Abstractions.Processing;
using Microsoft.ML.Tokenizers;

namespace Lexora.Infrastructure.DocumentProcessing.Tokenization;

public sealed class TiktokenTokenCounter : ITokenCounter
{
    public const string Encoding = "o200k_base";

    private readonly TiktokenTokenizer _tokenizer = TiktokenTokenizer.CreateForEncoding(Encoding);

    public int Count(string text) => string.IsNullOrEmpty(text) ? 0 : _tokenizer.CountTokens(text);
}
