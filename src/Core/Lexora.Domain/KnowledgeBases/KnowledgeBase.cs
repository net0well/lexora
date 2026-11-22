using Lexora.Domain.KnowledgeBases.Events;
using Lexora.Shared.Domain;
using Lexora.Shared.Guards;
using Lexora.Shared.Results;

namespace Lexora.Domain.KnowledgeBases;

public sealed class KnowledgeBase : AggregateRoot
{
    public const int DescriptionMaxLength = 1000;

    private static readonly Notification _archived =
        Notification.BusinessRule("knowledgeBase.archived", "The knowledge base is archived and does not accept new documents.");

    private KnowledgeBase()
    {
    }

    private KnowledgeBase(KnowledgeBaseName name, string? description, ChunkingSettings chunking, Guid createdBy, DateTimeOffset now)
        : base(Guid.CreateVersion7())
    {
        Name = name;
        Description = description;
        Chunking = chunking;
        Status = KnowledgeBaseStatus.Active;
        CreatedBy = createdBy;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public KnowledgeBaseName Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public ChunkingSettings Chunking { get; private set; } = null!;

    public KnowledgeBaseStatus Status { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsArchived => Status == KnowledgeBaseStatus.Archived;

    public static Result<KnowledgeBase> Create(
        string? name,
        string? description,
        ChunkingSettings chunking,
        Guid createdBy,
        DateTimeOffset now)
    {
        Guard.NotNull(chunking);
        Guard.NotEmpty(createdBy);

        var nameResult = KnowledgeBaseName.Create(name);
        var descriptionResult = NormalizeDescription(description);

        if (nameResult.IsFailure || descriptionResult.IsFailure)
        {
            return Result.Failure<KnowledgeBase>([.. nameResult.Notifications, .. descriptionResult.Notifications]);
        }

        return new KnowledgeBase(nameResult.Value, descriptionResult.Value, chunking, createdBy, now);
    }

    public Result Rename(string? name, DateTimeOffset now)
    {
        var nameResult = KnowledgeBaseName.Create(name);

        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Notifications);
        }

        Name = nameResult.Value;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result UpdateDescription(string? description, DateTimeOffset now)
    {
        var descriptionResult = NormalizeDescription(description);

        if (descriptionResult.IsFailure)
        {
            return Result.Failure(descriptionResult.Notifications);
        }

        Description = descriptionResult.Value;
        UpdatedAt = now;

        return Result.Success();
    }

    public void ChangeChunking(ChunkingSettings chunking, DateTimeOffset now)
    {
        Chunking = Guard.NotNull(chunking);
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        if (IsArchived)
        {
            return;
        }

        Status = KnowledgeBaseStatus.Archived;
        UpdatedAt = now;
        Raise(new KnowledgeBaseArchived(Id, now));
    }

    public Result EnsureAcceptsUploads() => IsArchived ? Result.Failure(_archived) : Result.Success();

    private static Result<string?> NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return Result.Success<string?>(null);
        }

        return trimmed.Length <= DescriptionMaxLength
            ? trimmed
            : Result.Failure<string?>(Notification.Validation(
                "knowledgeBase.description.tooLong",
                $"The description cannot be longer than {DescriptionMaxLength} characters."));
    }
}
