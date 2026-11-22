using Asp.Versioning;
using Lexora.Api.Contracts;
using Lexora.Api.Contracts.Requests;
using Lexora.Api.Mappers;
using Lexora.Api.Results;
using Lexora.Application.Common.Paging;
using Lexora.Application.KnowledgeBases.ArchiveKnowledgeBase;
using Lexora.Application.KnowledgeBases.CreateKnowledgeBase;
using Lexora.Application.KnowledgeBases.GetKnowledgeBase;
using Lexora.Application.KnowledgeBases.ListKnowledgeBases;
using Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;
using Lexora.Domain.KnowledgeBases;
using Lexora.Infrastructure.Identity;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Lexora.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/knowledge-bases")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.RequireViewer)]
[ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<CommandResult>(StatusCodes.Status403Forbidden)]
public sealed class KnowledgeBasesController(IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CommandResult<PagedResponse<KnowledgeBaseListItemResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] KnowledgeBaseStatus? status,
        CancellationToken cancellationToken,
        [FromQuery] int page = PageRequest.DefaultPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize)
    {
        var query = new ListKnowledgeBasesQuery(status, page, pageSize);

        return (await bus.InvokeAsync<Result<PagedResponse<KnowledgeBaseListItemResponse>>>(query, cancellationToken)).ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CommandResult<GetKnowledgeBaseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<GetKnowledgeBaseResponse>>(new GetKnowledgeBaseQuery(id), cancellationToken)).ToActionResult();

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType<CommandResult<CreateKnowledgeBaseResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateKnowledgeBaseRequest request, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<CreateKnowledgeBaseResponse>>(request.ToCommand(), cancellationToken))
            .ToCreatedResult(knowledgeBase => Url.Action(nameof(Get), new { id = knowledgeBase.Id }));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType<CommandResult<UpdateKnowledgeBaseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateKnowledgeBaseRequest request, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<UpdateKnowledgeBaseResponse>>(request.ToCommand(id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result>(new ArchiveKnowledgeBaseCommand(id), cancellationToken)).ToActionResult();
}
