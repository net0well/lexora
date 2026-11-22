using Lexora.Shared.Domain;

namespace Lexora.Domain.Documents.Events;

public sealed record DocumentTextExtracted(
    Guid KnowledgeBaseId,
    Guid DocumentId,
    int ProcessingVersion,
    DateTimeOffset OccurredAt) : IDomainEvent;
