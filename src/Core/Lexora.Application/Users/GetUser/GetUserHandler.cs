using Lexora.Application.Users.Abstractions;
using Lexora.Shared.Results;

namespace Lexora.Application.Users.GetUser;

public static class GetUserHandler
{
    public static async Task<Result<UserResponse>> Handle(GetUserQuery query, IUserQueries queries, CancellationToken cancellationToken)
    {
        var user = await queries.GetByIdAsync(query.UserId, cancellationToken);

        return user is null ? Result.Failure<UserResponse>(UserNotifications.NotFound) : user;
    }
}
