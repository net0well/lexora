using Lexora.Domain.Users;

namespace Lexora.Application.Auth;

public sealed record AuthenticatedUserResponse(Guid Id, string Email, string Name, UserRole Role);
