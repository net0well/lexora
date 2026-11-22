using Lexora.Shared.Domain;

namespace Lexora.Domain.KnowledgeBases.Events;

public sealed record KnowledgeBaseArchived(Guid KnowledgeBaseId, DateTimeOffset OccurredAt) : IDomainEvent;
