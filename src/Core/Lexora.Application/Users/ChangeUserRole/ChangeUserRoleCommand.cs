using Lexora.Domain.Users;

namespace Lexora.Application.Users.ChangeUserRole;

public sealed record ChangeUserRoleCommand(Guid UserId, UserRole Role);
