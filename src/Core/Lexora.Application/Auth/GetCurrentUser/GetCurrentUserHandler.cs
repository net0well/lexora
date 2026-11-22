using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Auth.Mappers;
using Lexora.Domain.Users;
using Lexora.Shared.Results;

namespace Lexora.Application.Auth.GetCurrentUser;

public static class GetCurrentUserHandler
{
    public static async Task<Result<AuthenticatedUserResponse>> Handle(
        GetCurrentUserQuery query,
        ICurrentUser currentUser,
        IUserRepository users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);

        return user is null
            ? Result.Failure<AuthenticatedUserResponse>(Notification.NotFound("user.notFound", "User not found."))
            : user.ToAuthenticatedUser();
    }
}
