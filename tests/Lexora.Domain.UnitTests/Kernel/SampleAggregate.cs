using Lexora.Shared.Domain;

namespace Lexora.Domain.UnitTests.Kernel;

internal sealed class SampleAggregate() : AggregateRoot(Guid.CreateVersion7())
{
    public void MarkHappened(DateTimeOffset now) => Raise(new SampleHappened(Id, now));
}
