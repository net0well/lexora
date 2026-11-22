using Lexora.Application.Users.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Users.ActivateUser;

public static class ActivateUserHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<UserResponse>> Handle(ActivateUserCommand command, IUserService service, CancellationToken cancellationToken) =>
        service.ActivateAsync(command, cancellationToken);
}
