using Lexora.Domain.Users;

namespace Lexora.Application.Auth.Mappers;

public static class AuthMapper
{
    public static AuthenticatedUserResponse ToAuthenticatedUser(this User user) => new(user.Id, user.Email.Value, user.Name.Value, user.Role);
}
