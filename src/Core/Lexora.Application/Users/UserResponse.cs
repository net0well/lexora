using Lexora.Domain.Users;

namespace Lexora.Application.Users;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string Name,
    UserRole Role,
    UserStatus Status,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
