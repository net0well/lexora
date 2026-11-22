using Lexora.Application.Users.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Users.DeactivateUser;

public static class DeactivateUserHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<UserResponse>> Handle(DeactivateUserCommand command, IUserService service, CancellationToken cancellationToken) =>
        service.DeactivateAsync(command, cancellationToken);
}
