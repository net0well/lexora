using Lexora.Application.Auth.Login;
using Lexora.Shared.Results;

namespace Lexora.Application.Auth.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
}
