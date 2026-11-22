using Lexora.Shared.Domain;

namespace Lexora.Domain.UnitTests.Kernel;

internal sealed class OtherSampleRange(int start, int? end) : ValueObject
{
    public int Start { get; } = start;

    public int? End { get; } = end;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
