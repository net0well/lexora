using Lexora.Application.Common.Paging;
using Lexora.Application.Users.Abstractions;
using Lexora.Shared.Results;

namespace Lexora.Application.Users.ListUsers;

public static class ListUsersHandler
{
    public static async Task<Result<PagedResponse<UserResponse>>> Handle(ListUsersQuery query, IUserQueries queries, CancellationToken cancellationToken) =>
        await queries.ListAsync(query, cancellationToken);
}
