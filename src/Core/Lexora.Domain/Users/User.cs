using Lexora.Shared.Domain;
using Lexora.Shared.Guards;
using Lexora.Shared.Results;

namespace Lexora.Domain.Users;

public sealed class User : AggregateRoot
{
    private User()
    {
    }

    private User(Email email, PersonName name, string passwordHash, UserRole role, DateTimeOffset now)
        : base(Guid.CreateVersion7())
    {
        Email = email;
        Name = name;
        PasswordHash = passwordHash;
        Role = role;
        Status = UserStatus.Active;
        PasswordChangedAt = now;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Email Email { get; private set; } = null!;

    public PersonName Name { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    public UserStatus Status { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockoutEndsAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset PasswordChangedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsActive => Status == UserStatus.Active;

    public static Result<User> Create(string? email, string? name, string passwordHash, UserRole role, DateTimeOffset now)
    {
        Guard.NotNullOrWhiteSpace(passwordHash);
        Guard.Defined(role);

        var emailResult = Email.Create(email);
        var nameResult = PersonName.Create(name);

        if (emailResult.IsFailure || nameResult.IsFailure)
        {
            return Result.Failure<User>([.. emailResult.Notifications, .. nameResult.Notifications]);
        }

        return new User(emailResult.Value, nameResult.Value, passwordHash, role, now);
    }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEndsAt > now;

    public bool CanSignIn(DateTimeOffset now) => IsActive && !IsLockedOut(now);

    public void RegisterFailedLogin(DateTimeOffset now, int maxAttempts, TimeSpan lockoutDuration)
    {
        Guard.Positive(maxAttempts);
        Guard.Positive(lockoutDuration);

        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEndsAt = now + lockoutDuration;
            FailedLoginAttempts = 0;
        }

        UpdatedAt = now;
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginAttempts = 0;
        LockoutEndsAt = null;
        LastLoginAt = now;
        UpdatedAt = now;
    }

    public void ChangePassword(string newPasswordHash, DateTimeOffset now)
    {
        PasswordHash = Guard.NotNullOrWhiteSpace(newPasswordHash);
        PasswordChangedAt = now;
        UpdatedAt = now;
    }

    public void ChangeRole(UserRole role, DateTimeOffset now)
    {
        Role = Guard.Defined(role);
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        Status = UserStatus.Inactive;
        UpdatedAt = now;
    }

    public void Activate(DateTimeOffset now)
    {
        Status = UserStatus.Active;
        UpdatedAt = now;
    }
}
