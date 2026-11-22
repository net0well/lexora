using Lexora.Application.Abstractions.Identity;
using Lexora.Application.KnowledgeBases.ArchiveKnowledgeBase;
using Lexora.Application.KnowledgeBases.CreateKnowledgeBase;
using Lexora.Application.KnowledgeBases.Mappers;
using Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;
using Lexora.Domain.KnowledgeBases;
using Lexora.Shared.Results;

namespace Lexora.Application.KnowledgeBases.Services;

public sealed class KnowledgeBaseService(
    IKnowledgeBaseRepository repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IKnowledgeBaseService
{
    public async Task<Result<CreateKnowledgeBaseResponse>> CreateAsync(CreateKnowledgeBaseCommand command, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByNameAsync(command.Name, cancellationToken))
        {
            return Result.Failure<CreateKnowledgeBaseResponse>(KnowledgeBaseNotifications.NameDuplicate);
        }

        var chunking = ChunkingSettings.Create(
            command.ChunkMaxTokens ?? ChunkingSettings.DefaultMaxTokens,
            command.ChunkOverlapTokens ?? ChunkingSettings.DefaultOverlapTokens);

        if (chunking.IsFailure)
        {
            return Result.Failure<CreateKnowledgeBaseResponse>(chunking.Notifications);
        }

        var created = KnowledgeBase.Create(command.Name, command.Description, chunking.Value, currentUser.UserId, timeProvider.GetUtcNow());

        if (created.IsFailure)
        {
            return Result.Failure<CreateKnowledgeBaseResponse>(created.Notifications);
        }

        await repository.AddAsync(created.Value, cancellationToken);

        return created.Value.ToCreateResponse();
    }

    public async Task<Result<UpdateKnowledgeBaseResponse>> UpdateAsync(UpdateKnowledgeBaseCommand command, CancellationToken cancellationToken)
    {
        var knowledgeBase = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (knowledgeBase is null)
        {
            return Result.Failure<UpdateKnowledgeBaseResponse>(KnowledgeBaseNotifications.NotFound);
        }

        var renamed = !string.Equals(knowledgeBase.Name.Value, command.Name.Trim(), StringComparison.OrdinalIgnoreCase);

        if (renamed && await repository.ExistsByNameAsync(command.Name, cancellationToken))
        {
            return Result.Failure<UpdateKnowledgeBaseResponse>(KnowledgeBaseNotifications.NameDuplicate);
        }

        var chunking = ChunkingSettings.Create(
            command.ChunkMaxTokens ?? knowledgeBase.Chunking.MaxTokens,
            command.ChunkOverlapTokens ?? knowledgeBase.Chunking.OverlapTokens);

        if (chunking.IsFailure)
        {
            return Result.Failure<UpdateKnowledgeBaseResponse>(chunking.Notifications);
        }

        var now = timeProvider.GetUtcNow();
        var rename = knowledgeBase.Rename(command.Name, now);
        var describe = knowledgeBase.UpdateDescription(command.Description, now);

        if (rename.IsFailure || describe.IsFailure)
        {
            return Result.Failure<UpdateKnowledgeBaseResponse>([.. rename.Notifications, .. describe.Notifications]);
        }

        knowledgeBase.ChangeChunking(chunking.Value, now);

        return knowledgeBase.ToUpdateResponse();
    }

    public async Task<Result> ArchiveAsync(ArchiveKnowledgeBaseCommand command, CancellationToken cancellationToken)
    {
        var knowledgeBase = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (knowledgeBase is null)
        {
            return Result.Failure(KnowledgeBaseNotifications.NotFound);
        }

        knowledgeBase.Archive(timeProvider.GetUtcNow());

        return Result.Success();
    }
}
