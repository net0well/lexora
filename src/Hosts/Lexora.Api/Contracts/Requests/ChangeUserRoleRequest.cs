using Lexora.Domain.Users;

namespace Lexora.Api.Contracts.Requests;

public sealed record ChangeUserRoleRequest(UserRole Role);
