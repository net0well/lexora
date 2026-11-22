using Lexora.Application.Users.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Users.ChangeUserRole;

public static class ChangeUserRoleHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<UserResponse>> Handle(ChangeUserRoleCommand command, IUserService service, CancellationToken cancellationToken) =>
        service.ChangeRoleAsync(command, cancellationToken);
}
