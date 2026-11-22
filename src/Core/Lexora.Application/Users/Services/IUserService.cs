using Lexora.Application.Users.ActivateUser;
using Lexora.Application.Users.ChangeUserRole;
using Lexora.Application.Users.CreateUser;
using Lexora.Application.Users.DeactivateUser;
using Lexora.Application.Users.ResetUserPassword;
using Lexora.Shared.Results;

namespace Lexora.Application.Users.Services;

public interface IUserService
{
    Task<Result<UserResponse>> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken);

    Task<Result<UserResponse>> ChangeRoleAsync(ChangeUserRoleCommand command, CancellationToken cancellationToken);

    Task<Result<UserResponse>> DeactivateAsync(DeactivateUserCommand command, CancellationToken cancellationToken);

    Task<Result<UserResponse>> ActivateAsync(ActivateUserCommand command, CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(ResetUserPasswordCommand command, CancellationToken cancellationToken);
}
