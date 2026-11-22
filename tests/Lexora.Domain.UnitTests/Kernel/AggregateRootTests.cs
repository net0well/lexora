using Lexora.Shared.Domain;

namespace Lexora.Domain.UnitTests.Kernel;

public sealed class AggregateRootTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DomainEvents_NewAggregate_IsEmpty()
    {
        var aggregate = new SampleAggregate();

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Raise_TwoEvents_KeepsThemInOrder()
    {
        var aggregate = new SampleAggregate();

        aggregate.MarkHappened(_now);
        aggregate.MarkHappened(_now.AddMinutes(1));

        aggregate.DomainEvents.ShouldBe(
        [
            new SampleHappened(aggregate.Id, _now),
            new SampleHappened(aggregate.Id, _now.AddMinutes(1)),
        ]);
    }

    [Fact]
    public void ClearDomainEvents_WithRaisedEvents_RemovesAll()
    {
        var aggregate = new SampleAggregate();
        aggregate.MarkHappened(_now);

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_CastToMutableCollection_RejectsAdditions()
    {
        var aggregate = new SampleAggregate();

        var events = aggregate.DomainEvents.ShouldBeAssignableTo<ICollection<IDomainEvent>>();

        Should.Throw<NotSupportedException>(() => events.Add(new SampleHappened(aggregate.Id, _now)));
    }
}
