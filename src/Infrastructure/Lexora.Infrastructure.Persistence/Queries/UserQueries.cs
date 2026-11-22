using System.Linq.Expressions;
using Lexora.Application.Common.Paging;
using Lexora.Application.Users;
using Lexora.Application.Users.Abstractions;
using Lexora.Application.Users.ListUsers;
using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Queries;

public sealed class UserQueries(LexoraDbContext context) : IUserQueries
{
    private static readonly Expression<Func<User, UserResponse>> _toResponse = user => new UserResponse(
        user.Id,
        user.Email.Value,
        user.Name.Value,
        user.Role,
        user.Status,
        user.LastLoginAt,
        user.CreatedAt,
        user.UpdatedAt);

    public Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(_toResponse)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResponse<UserResponse>> ListAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var users = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search.Trim());

            // Email and Name use value converters; the object cast makes EF compare the stored text column.
            users = users.Where(user =>
                EF.Functions.ILike((string)(object)user.Email, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike((string)(object)user.Name, pattern, LikePattern.EscapeCharacter));
        }

        if (query.Role is { } role)
        {
            users = users.Where(user => user.Role == role);
        }

        if (query.Status is { } status)
        {
            users = users.Where(user => user.Status == status);
        }

        var paging = query.Paging;
        var totalItems = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(_toResponse)
            .ToListAsync(cancellationToken);

        return PagedResponse.Create<UserResponse>(items, paging, totalItems);
    }
}
