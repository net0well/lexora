using Lexora.Domain.Users;

namespace Lexora.Application.Abstractions.Identity;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid UserId { get; }

    UserRole Role { get; }
}
