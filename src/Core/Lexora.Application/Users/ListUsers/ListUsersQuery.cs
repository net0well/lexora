using Lexora.Application.Common.Paging;
using Lexora.Domain.Users;

namespace Lexora.Application.Users.ListUsers;

public sealed record ListUsersQuery(
    string? Search,
    UserRole? Role,
    UserStatus? Status,
    int Page = PageRequest.DefaultPage,
    int PageSize = PageRequest.DefaultPageSize)
{
    public PageRequest Paging => new(Page, PageSize);
}
