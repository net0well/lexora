using Lexora.Domain.Users;

namespace Lexora.TestKit.Builders;

public sealed class UserBuilder
{
    public const string DefaultPasswordHash = "hashed-password";

    private string _email = "ana.souza@lexora.local";
    private string _name = "Ana Souza";
    private string _passwordHash = DefaultPasswordHash;
    private UserRole _role = UserRole.Member;
    private DateTimeOffset _now = TestClock.Now;
    private bool _inactive;

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public UserBuilder CreatedAt(DateTimeOffset now)
    {
        _now = now;
        return this;
    }

    public UserBuilder Inactive()
    {
        _inactive = true;
        return this;
    }

    public User Build()
    {
        var user = User.Create(_email, _name, _passwordHash, _role, _now).Value;

        if (_inactive)
        {
            user.Deactivate(_now);
        }

        user.ClearDomainEvents();

        return user;
    }
}
