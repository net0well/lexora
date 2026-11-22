namespace Lexora.Shared.Domain;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
