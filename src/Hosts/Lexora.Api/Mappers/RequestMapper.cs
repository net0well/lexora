using Lexora.Api.Contracts.Requests;
using Lexora.Application.Auth.ChangePassword;
using Lexora.Application.Auth.Login;
using Lexora.Application.Users.ChangeUserRole;
using Lexora.Application.Users.CreateUser;
using Lexora.Application.Users.ResetUserPassword;

namespace Lexora.Api.Mappers;

public static class RequestMapper
{
    public static LoginCommand ToCommand(this LoginRequest request) => new(request.Email, request.Password);

    public static ChangePasswordCommand ToCommand(this ChangePasswordRequest request) => new(request.CurrentPassword, request.NewPassword);

    public static CreateUserCommand ToCommand(this CreateUserRequest request) => new(request.Email, request.Name, request.Role, request.Password);

    public static ChangeUserRoleCommand ToCommand(this ChangeUserRoleRequest request, Guid userId) => new(userId, request.Role);

    public static ResetUserPasswordCommand ToCommand(this ResetUserPasswordRequest request, Guid userId) => new(userId, request.NewPassword);
}
