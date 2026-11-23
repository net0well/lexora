using Asp.Versioning;
using Lexora.Api.Contracts;
using Lexora.Api.Contracts.Requests;
using Lexora.Api.Mappers;
using Lexora.Api.Results;
using Lexora.Application.Common.Paging;
using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.DeleteConversation;
using Lexora.Application.Conversations.GetConversation;
using Lexora.Application.Conversations.ListConversations;
using Lexora.Application.Conversations.StartConversation;
using Lexora.Infrastructure.Identity;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Lexora.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/conversations")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.RequireViewer)]
[ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<CommandResult>(StatusCodes.Status403Forbidden)]
public sealed class ConversationsController(IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CommandResult<PagedResponse<ConversationListItemResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? knowledgeBaseId,
        CancellationToken cancellationToken,
        [FromQuery] int page = PageRequest.DefaultPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize) =>
        (await bus.InvokeAsync<Result<PagedResponse<ConversationListItemResponse>>>(
            new ListConversationsQuery(knowledgeBaseId, page, pageSize),
            cancellationToken)).ToActionResult();

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CommandResult<GetConversationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<GetConversationResponse>>(new GetConversationQuery(id), cancellationToken)).ToActionResult();

    [HttpPost]
    [ProducesResponseType<CommandResult<StartConversationResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Start(StartConversationRequest request, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<StartConversationResponse>>(request.ToCommand(), cancellationToken))
            .ToCreatedResult(conversation => Url.Action(nameof(Get), new { id = conversation.Id }));

    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType<CommandResult<AskQuestionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ask(Guid id, AskQuestionRequest request, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<AskQuestionResponse>>(request.ToCommand(id), cancellationToken)).ToActionResult();

    [HttpDelete("{id:guid}")]
    [ProducesResponseType<CommandResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result>(new DeleteConversationCommand(id), cancellationToken)).ToActionResult();
}
