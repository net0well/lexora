using Asp.Versioning;
using Lexora.Api.Contracts;
using Lexora.Api.Contracts.Requests;
using Lexora.Api.Mappers;
using Lexora.Api.Results;
using Lexora.Api.Streaming;
using Lexora.Application.Common.Paging;
using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.DeleteConversation;
using Lexora.Application.Conversations.GetConversation;
using Lexora.Application.Conversations.ListConversations;
using Lexora.Application.Conversations.Services;
using Lexora.Application.Conversations.StartConversation;
using Lexora.Application.Conversations.Streaming;
using Lexora.Infrastructure.Identity;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wolverine;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

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

    [HttpPost("{id:guid}/messages/stream")]
    [Produces("text/event-stream", "application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK, "text/event-stream")]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IResult> Stream(
        Guid id,
        AskQuestionRequest request,
        [FromServices] IAnswerStreamingService streaming,
        [FromServices] IOptions<HttpJsonOptions> json,
        [FromServices] ILogger<ConversationsController> logger,
        CancellationToken cancellationToken)
    {
        var events = streaming.StreamAsync(request.ToCommand(id), cancellationToken).GetAsyncEnumerator(cancellationToken);

        // Failures found before any text (invalid question, unknown conversation) still get a regular status code.
        if (!await events.MoveNextAsync() || events.Current is AnswerErrorEvent)
        {
            var notifications = events.Current is AnswerErrorEvent error ? error.Notifications : [];
            await events.DisposeAsync();

            return TypedResults.Json(CommandResult.Fail(notifications), json.Value.SerializerOptions, statusCode: NotificationStatusCodeMapper.ToStatusCode(notifications));
        }

        return TypedResults.ServerSentEvents(AnswerSseEvents.From(events, json.Value.SerializerOptions, logger, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType<CommandResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result>(new DeleteConversationCommand(id), cancellationToken)).ToActionResult();
}
