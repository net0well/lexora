using System.Security.Cryptography;
using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Abstractions.Storage;
using Lexora.Application.Common.Options;
using Lexora.Application.Documents.DeleteDocument;
using Lexora.Application.Documents.ReprocessDocument;
using Lexora.Application.Documents.UploadDocument;
using Lexora.Application.KnowledgeBases;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.Services;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Documents.Services;

public sealed class DocumentService(
    IDocumentRepository documents,
    IKnowledgeBaseRepository knowledgeBases,
    IDocumentTextExtractor extractor,
    IFileStorage storage,
    ICurrentUser currentUser,
    IOptions<DocumentOptions> options,
    TimeProvider timeProvider) : IDocumentService
{
    private const int BufferSize = 81920;

    public async Task<Result<UploadDocumentResponse>> UploadAsync(UploadDocumentCommand command, CancellationToken cancellationToken)
    {
        var knowledgeBase = await knowledgeBases.GetByIdAsync(command.KnowledgeBaseId, cancellationToken);

        if (knowledgeBase is null)
        {
            return Result.Failure<UploadDocumentResponse>(KnowledgeBaseNotifications.NotFound);
        }

        var fileName = FileName.Create(command.FileName);

        if (fileName.IsFailure)
        {
            return Result.Failure<UploadDocumentResponse>(fileName.Notifications);
        }

        await using var buffered = command.Content.CanSeek ? null : await BufferAsync(command.Content, cancellationToken);
        var content = buffered ?? command.Content;
        content.Position = 0;

        var format = await extractor.DetectFormatAsync(content, fileName.Value.Value, cancellationToken);
        var limits = UploadLimits.Create(options.Value.MaxFileSizeBytes, options.Value.MaxTotalStorageBytes);
        var allowed = DocumentUploadPolicy.Evaluate(
            knowledgeBase,
            format,
            content.Length,
            limits,
            await documents.SumActiveSizeAsync(cancellationToken));

        if (allowed.IsFailure)
        {
            return Result.Failure<UploadDocumentResponse>(allowed.Notifications);
        }

        var hash = await HashAsync(content, cancellationToken);

        if (await documents.ExistsWithHashAsync(knowledgeBase.Id, hash, cancellationToken))
        {
            return Result.Failure<UploadDocumentResponse>(DocumentNotifications.Duplicate);
        }

        return await StoreAsync(knowledgeBase, fileName.Value.Value, format!.Value, hash, content, cancellationToken);
    }

    public async Task<Result> DeleteAsync(DeleteDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(command.Id, cancellationToken);

        return document is null ? Result.Failure(DocumentNotifications.NotFound) : document.Delete(timeProvider.GetUtcNow());
    }

    public async Task<Result<ReprocessDocumentResponse>> ReprocessAsync(ReprocessDocumentCommand command, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(command.Id, cancellationToken);

        if (document is null)
        {
            return Result.Failure<ReprocessDocumentResponse>(DocumentNotifications.NotFound);
        }

        var requested = document.RequestReprocess(timeProvider.GetUtcNow());

        return requested.IsFailure
            ? Result.Failure<ReprocessDocumentResponse>(requested.Notifications)
            : new ReprocessDocumentResponse(document.Id, document.Status, document.ProcessingVersion);
    }

    private async Task<Result<UploadDocumentResponse>> StoreAsync(
        KnowledgeBase knowledgeBase,
        string fileName,
        DocumentFormat format,
        ContentHash hash,
        Stream content,
        CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var contentType = DocumentFormats.ContentTypeOf(format);
        var stored = await storage.SaveAsync(DocumentStorageKeys.OriginalOf(knowledgeBase.Id, id, format), content, contentType, cancellationToken);

        var document = Document.Upload(
            id,
            knowledgeBase.Id,
            fileName,
            format,
            contentType,
            stored.SizeBytes,
            hash,
            stored.Key,
            currentUser.UserId,
            timeProvider.GetUtcNow()).Value;

        await documents.AddAsync(document, cancellationToken);

        return new UploadDocumentResponse(document.Id, document.FileName.Value, document.Format, document.Status, document.SizeBytes);
    }

    private static async Task<FileStream> BufferAsync(Stream source, CancellationToken cancellationToken)
    {
        var buffer = new FileStream(
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.DeleteOnClose);

        await source.CopyToAsync(buffer, cancellationToken);

        return buffer;
    }

    private static async Task<ContentHash> HashAsync(Stream content, CancellationToken cancellationToken)
    {
        content.Position = 0;
        var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(content, cancellationToken));
        content.Position = 0;

        return ContentHash.FromHex(hash);
    }
}
