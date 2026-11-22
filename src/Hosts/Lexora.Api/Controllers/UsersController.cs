using Asp.Versioning;
using Lexora.Api.Contracts;
using Lexora.Api.Contracts.Requests;
using Lexora.Api.Mappers;
using Lexora.Api.Results;
using Lexora.Application.Common.Paging;
using Lexora.Application.Users;
using Lexora.Application.Users.ActivateUser;
using Lexora.Application.Users.DeactivateUser;
using Lexora.Application.Users.GetUser;
using Lexora.Application.Users.ListUsers;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Identity;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Lexora.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/users")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
[ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<CommandResult>(StatusCodes.Status403Forbidden)]
public sealed class UsersController(IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CommandResult<PagedResponse<UserResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] UserRole? role,
        [FromQuery] UserStatus? status,
        CancellationToken cancellationToken,
        [FromQuery] int page = PageRequest.DefaultPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize)
    {
        var query = new ListUsersQuery(search, role, status, page, pageSize);

        return (await bus.InvokeAsync<Result<PagedResponse<UserResponse>>>(query, cancellationToken)).ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CommandResult<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<UserResponse>>(new GetUserQuery(id), cancellationToken)).ToActionResult();

    [HttpPost]
    [ProducesResponseType<CommandResult<UserResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<UserResponse>>(request.ToCommand(), cancellationToken))
            .ToCreatedResult(user => Url.Action(nameof(Get), new { id = user.Id }));

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType<CommandResult<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeUserRoleRequest request, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<UserResponse>>(request.ToCommand(id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType<CommandResult<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<UserResponse>>(new DeactivateUserCommand(id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType<CommandResult<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<UserResponse>>(new ActivateUserCommand(id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType<CommandResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, ResetUserPasswordRequest request, CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result>(request.ToCommand(id), cancellationToken)).ToActionResult();
}
