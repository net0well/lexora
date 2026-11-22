using System.Security.Claims;
using Lexora.Application.Abstractions.Identity;
using Lexora.Domain.Users;
using Lexora.Shared.Helpers;
using Microsoft.AspNetCore.Http;

namespace Lexora.Infrastructure.Identity;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public bool IsAuthenticated => AuthenticatedPrincipal is not null;

    public Guid UserId =>
        Guid.TryParse(AuthenticatedPrincipal?.FindFirstValue(ClaimNames.Subject), out var userId)
            ? userId
            : throw new InvalidOperationException("There is no authenticated user in the current context.");

    public UserRole Role =>
        EnumHelper.TryParse<UserRole>(AuthenticatedPrincipal?.FindFirstValue(ClaimNames.Role), out var role)
            ? role
            : throw new InvalidOperationException("There is no authenticated user in the current context.");

    private ClaimsPrincipal? AuthenticatedPrincipal =>
        httpContextAccessor.HttpContext?.User is { Identity.IsAuthenticated: true } principal ? principal : null;
}
