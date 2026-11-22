using Lexora.Application.Auth.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Auth.Logout;

public static class LogoutHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result> Handle(LogoutCommand command, IAuthService service, CancellationToken cancellationToken) =>
        service.LogoutAsync(command, cancellationToken);
}
