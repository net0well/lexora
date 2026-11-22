using Asp.Versioning;
using Lexora.Api.Auth;
using Lexora.Api.Contracts;
using Lexora.Api.Contracts.Requests;
using Lexora.Api.Contracts.Responses;
using Lexora.Api.Mappers;
using Lexora.Api.RateLimiting;
using Lexora.Api.Results;
using Lexora.Application.Auth;
using Lexora.Application.Auth.GetCurrentUser;
using Lexora.Application.Auth.Login;
using Lexora.Application.Auth.Logout;
using Lexora.Application.Auth.RefreshSession;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wolverine;

namespace Lexora.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthController(IMessageBus bus, RefreshTokenCookie refreshTokenCookie) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<CommandResult<SessionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result<LoginResponse>>(request.ToCommand(), cancellationToken);

        if (result.IsSuccess)
        {
            refreshTokenCookie.Append(Response, result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt);
        }

        return result.ToActionResult(SessionMapper.ToSession);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Refresh)]
    [ProducesResponseType<CommandResult<SessionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var command = new RefreshSessionCommand(refreshTokenCookie.Read(Request));
        var result = await bus.InvokeAsync<Result<RefreshSessionResponse>>(command, cancellationToken);

        if (result.IsSuccess)
        {
            refreshTokenCookie.Append(Response, result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt);
        }
        else
        {
            refreshTokenCookie.Delete(Response);
        }

        return result.ToActionResult(SessionMapper.ToSession);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType<CommandResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result>(new LogoutCommand(refreshTokenCookie.Read(Request)), cancellationToken);
        refreshTokenCookie.Delete(Response);

        return result.ToActionResult();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CommandResult<AuthenticatedUserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<AuthenticatedUserResponse>>(new GetCurrentUserQuery(), cancellationToken)).ToActionResult();

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType<CommandResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<Result>(request.ToCommand(), cancellationToken);

        if (result.IsSuccess)
        {
            refreshTokenCookie.Delete(Response);
        }

        return result.ToActionResult();
    }
}
