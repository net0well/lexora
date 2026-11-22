using Lexora.Shared.Domain;

namespace Lexora.Domain.Documents.Events;

public sealed record DocumentUploaded(
    Guid KnowledgeBaseId,
    Guid DocumentId,
    int ProcessingVersion,
    DateTimeOffset OccurredAt) : IDomainEvent;
