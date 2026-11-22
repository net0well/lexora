using Lexora.Application.Auth.Login;
using Lexora.Application.Auth.RefreshSession;
using Lexora.Shared.Results;

namespace Lexora.Application.Auth.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginCommand command, CancellationToken cancellationToken);

    Task<Result<RefreshSessionResponse>> RefreshAsync(RefreshSessionCommand command, CancellationToken cancellationToken);
}
