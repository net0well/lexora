using Lexora.Shared.Domain;
using Lexora.Shared.Guards;

namespace Lexora.Domain.Conversations;

public sealed class TokenUsage : ValueObject
{
    private TokenUsage(int inputTokens, int outputTokens)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }

    public static TokenUsage None { get; } = new(0, 0);

    public int InputTokens { get; }

    public int OutputTokens { get; }

    public int TotalTokens => InputTokens + OutputTokens;

    public static TokenUsage Create(int inputTokens, int outputTokens) =>
        new(Guard.NotNegative(inputTokens), Guard.NotNegative(outputTokens));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return InputTokens;
        yield return OutputTokens;
    }
}
