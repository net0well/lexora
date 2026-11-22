using Lexora.Shared.Domain;

namespace Lexora.Domain.Documents.Events;

public sealed record DocumentReprocessRequested(
    Guid KnowledgeBaseId,
    Guid DocumentId,
    int ProcessingVersion,
    DateTimeOffset OccurredAt) : IDomainEvent;
