using Lexora.Api.Contracts.Requests;
using Lexora.Application.Auth.ChangePassword;
using Lexora.Application.Auth.Login;

namespace Lexora.Api.Mappers;

public static class RequestMapper
{
    public static LoginCommand ToCommand(this LoginRequest request) => new(request.Email, request.Password);

    public static ChangePasswordCommand ToCommand(this ChangePasswordRequest request) => new(request.CurrentPassword, request.NewPassword);
}
