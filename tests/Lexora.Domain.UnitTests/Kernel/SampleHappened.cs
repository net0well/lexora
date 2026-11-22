using Lexora.Shared.Domain;

namespace Lexora.Domain.UnitTests.Kernel;

internal sealed record SampleHappened(Guid AggregateId, DateTimeOffset OccurredAt) : IDomainEvent;
