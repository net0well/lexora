using Lexora.Application.Auth.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Auth.ChangePassword;

public static class ChangePasswordHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result> Handle(ChangePasswordCommand command, IAuthService service, CancellationToken cancellationToken) =>
        service.ChangePasswordAsync(command, cancellationToken);
}
