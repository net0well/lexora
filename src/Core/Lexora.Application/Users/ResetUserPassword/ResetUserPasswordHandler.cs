using Lexora.Application.Users.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Users.ResetUserPassword;

public static class ResetUserPasswordHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result> Handle(ResetUserPasswordCommand command, IUserService service, CancellationToken cancellationToken) =>
        service.ResetPasswordAsync(command, cancellationToken);
}
