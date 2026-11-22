using Lexora.Domain.Users;

namespace Lexora.Application.Users.Mappers;

public static class UserMapper
{
    public static UserResponse ToResponse(this User user) => new(
        user.Id,
        user.Email.Value,
        user.Name.Value,
        user.Role,
        user.Status,
        user.LastLoginAt,
        user.CreatedAt,
        user.UpdatedAt);
}
