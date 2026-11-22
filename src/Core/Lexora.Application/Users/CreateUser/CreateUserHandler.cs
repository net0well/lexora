using Lexora.Application.Users.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Users.CreateUser;

public static class CreateUserHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<UserResponse>> Handle(CreateUserCommand command, IUserService service, CancellationToken cancellationToken) =>
        service.CreateAsync(command, cancellationToken);
}
