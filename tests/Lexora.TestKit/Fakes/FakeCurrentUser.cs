using Lexora.Application.Abstractions.Identity;
using Lexora.Domain.Users;

namespace Lexora.TestKit.Fakes;

public sealed class FakeCurrentUser : ICurrentUser
{
    private Guid? _userId;
    private UserRole _role;

    public bool IsAuthenticated => _userId is not null;

    public Guid UserId => _userId ?? throw new InvalidOperationException("There is no authenticated user in the current context.");

    public UserRole Role => IsAuthenticated ? _role : throw new InvalidOperationException("There is no authenticated user in the current context.");

    public FakeCurrentUser SignIn(User user)
    {
        _userId = user.Id;
        _role = user.Role;

        return this;
    }

    public FakeCurrentUser SignOut()
    {
        _userId = null;

        return this;
    }
}
