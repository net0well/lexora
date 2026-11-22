using Lexora.Application.Auth.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Auth.Login;

public static class LoginHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<LoginResponse>> Handle(LoginCommand command, IAuthService service, CancellationToken cancellationToken) =>
        service.LoginAsync(command, cancellationToken);
}
