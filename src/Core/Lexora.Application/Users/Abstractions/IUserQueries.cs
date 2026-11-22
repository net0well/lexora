using Lexora.Application.Common.Paging;
using Lexora.Application.Users.ListUsers;

namespace Lexora.Application.Users.Abstractions;

public interface IUserQueries
{
    Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResponse<UserResponse>> ListAsync(ListUsersQuery query, CancellationToken cancellationToken);
}
