using Lexora.Api.Contracts;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Lexora.Api.Results;

public sealed class EnvelopeAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly Notification _unauthenticated =
        Notification.Unauthorized("auth.unauthenticated", "Authentication is required.");

    private static readonly Notification _forbidden =
        Notification.Forbidden("auth.forbidden", "You do not have permission to perform this action.");

    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            await context.ChallengeAsync();
            await context.Response.WriteAsJsonAsync(CommandResult.Fail([_unauthenticated]), context.RequestAborted);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await context.ForbidAsync();
            await context.Response.WriteAsJsonAsync(CommandResult.Fail([_forbidden]), context.RequestAborted);
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
