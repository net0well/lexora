using Asp.Versioning;
using Lexora.Api.Contracts;
using Lexora.Api.Results;
using Lexora.Application.Common.Paging;
using Lexora.Application.Documents.DeleteDocument;
using Lexora.Application.Documents.GetDocument;
using Lexora.Application.Documents.GetDocumentDownload;
using Lexora.Application.Documents.ListDocumentChunks;
using Lexora.Application.Documents.ListDocuments;
using Lexora.Application.Documents.ReprocessDocument;
using Lexora.Application.Documents.UploadDocument;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.Identity;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Lexora.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.RequireViewer)]
[ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<CommandResult>(StatusCodes.Status403Forbidden)]
public sealed class DocumentsController(IMessageBus bus) : ControllerBase
{
    private static readonly TimeSpan _uploadTimeout = TimeSpan.FromMinutes(5);

    private static readonly Notification _fileRequired =
        Notification.Validation("document.file.required", "Send the file in the 'file' field of a multipart form.");

    [HttpPost("knowledge-bases/{knowledgeBaseId:guid}/documents")]
    [Authorize(Policy = AuthorizationPolicies.RequireMember)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<CommandResult<UploadDocumentResponse>>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upload(Guid knowledgeBaseId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return Result.Failure<UploadDocumentResponse>(_fileRequired).ToActionResult();
        }

        await using var content = file.OpenReadStream();
        var command = new UploadDocumentCommand(knowledgeBaseId, file.FileName, file.ContentType, file.Length, content);

        var result = await bus.InvokeAsync<Result<UploadDocumentResponse>>(command, cancellationToken, _uploadTimeout);

        return result.ToAcceptedResult(document => Url.Action(nameof(Get), new { id = document.Id }));
    }

    [HttpGet("knowledge-bases/{knowledgeBaseId:guid}/documents")]
    [ProducesResponseType<CommandResult<PagedResponse<DocumentListItemResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        Guid knowledgeBaseId,
        [FromQuery] DocumentStatus? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken,
        [FromQuery] int page = PageRequest.DefaultPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize)
    {
        var query = new ListDocumentsQuery(knowledgeBaseId, status, search, page, pageSize);

        return (await bus.InvokeAsync<Result<PagedResponse<DocumentListItemResponse>>>(query, cancellationToken)).ToActionResult();
    }

    [HttpGet("documents/{id:guid}")]
    [ProducesResponseType<CommandResult<GetDocumentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<GetDocumentResponse>>(new GetDocumentQuery(id), cancellationToken)).ToActionResult();

    [HttpGet("documents/{id:guid}/chunks")]
    [ProducesResponseType<CommandResult<PagedResponse<DocumentChunkResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Chunks(
        Guid id,
        CancellationToken cancellationToken,
        [FromQuery] int page = PageRequest.DefaultPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        (await bus.InvokeAsync<Result<PagedResponse<DocumentChunkResponse>>>(new ListDocumentChunksQuery(id, page, pageSize), cancellationToken))
            .ToActionResult();

    [HttpGet("documents/{id:guid}/download")]
    [ProducesResponseType<CommandResult<DocumentDownloadResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<DocumentDownloadResponse>>(new GetDocumentDownloadQuery(id), cancellationToken)).ToActionResult();

    [HttpPost("documents/{id:guid}/reprocess")]
    [Authorize(Policy = AuthorizationPolicies.RequireMember)]
    [ProducesResponseType<CommandResult<ReprocessDocumentResponse>>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reprocess(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<ReprocessDocumentResponse>>(new ReprocessDocumentCommand(id), cancellationToken))
            .ToAcceptedResult(document => Url.Action(nameof(Get), new { id = document.Id }));

    [HttpDelete("documents/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireMember)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result>(new DeleteDocumentCommand(id), cancellationToken)).ToActionResult();
}
