using Lexora.Domain.Users;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Domain.UnitTests.Users;

public sealed class UserTests
{
    private const int MaxAttempts = 5;

    private static readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(15);

    private readonly FakeTimeProvider _time = new(TestClock.Now);

    [Fact]
    public void Create_ValidInput_CreatesActiveUserWithoutLoginHistory()
    {
        var result = User.Create(" Ana.Souza@Lexora.Local ", " Ana Souza ", "hash", UserRole.Admin, TestClock.Now);

        result.IsSuccess.ShouldBeTrue();
        var user = result.Value;
        user.Id.ShouldNotBe(Guid.Empty);
        user.Email.Value.ShouldBe("ana.souza@lexora.local");
        user.Name.Value.ShouldBe("Ana Souza");
        user.PasswordHash.ShouldBe("hash");
        user.Role.ShouldBe(UserRole.Admin);
        user.Status.ShouldBe(UserStatus.Active);
        user.FailedLoginAttempts.ShouldBe(0);
        user.LockoutEndsAt.ShouldBeNull();
        user.LastLoginAt.ShouldBeNull();
        user.PasswordChangedAt.ShouldBe(TestClock.Now);
        user.CreatedAt.ShouldBe(TestClock.Now);
        user.UpdatedAt.ShouldBe(TestClock.Now);
    }

    [Fact]
    public void Create_InvalidEmailAndName_ReportsBoth() =>
        User.Create("not-an-email", " ", "hash", UserRole.Member, TestClock.Now)
            .Notifications.Select(notification => notification.Key)
            .ShouldBe(["user.email.invalid", "user.name.required"]);

    [Fact]
    public void Create_BlankPasswordHash_Throws() =>
        Should.Throw<ArgumentException>(() => User.Create("ana@lexora.local", "Ana", " ", UserRole.Member, TestClock.Now));

    [Fact]
    public void Create_UndefinedRole_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => User.Create("ana@lexora.local", "Ana", "hash", (UserRole)99, TestClock.Now));

    [Fact]
    public void CanSignIn_ActiveUnlockedUser_ReturnsTrue() => new UserBuilder().Build().CanSignIn(_time.GetUtcNow()).ShouldBeTrue();

    [Fact]
    public void CanSignIn_InactiveUser_ReturnsFalse() => new UserBuilder().Inactive().Build().CanSignIn(_time.GetUtcNow()).ShouldBeFalse();

    [Fact]
    public void RegisterFailedLogin_BelowMaxAttempts_CountsWithoutLocking()
    {
        var user = new UserBuilder().Build();

        FailLogins(user, MaxAttempts - 1);

        user.FailedLoginAttempts.ShouldBe(MaxAttempts - 1);
        user.LockoutEndsAt.ShouldBeNull();
        user.CanSignIn(_time.GetUtcNow()).ShouldBeTrue();
    }

    [Fact]
    public void RegisterFailedLogin_WhenMaxAttemptsReached_LocksUser()
    {
        var user = new UserBuilder().Build();

        FailLogins(user, MaxAttempts);

        user.LockoutEndsAt.ShouldBe(_time.GetUtcNow() + _lockoutDuration);
        user.FailedLoginAttempts.ShouldBe(0);
        user.IsLockedOut(_time.GetUtcNow()).ShouldBeTrue();
        user.CanSignIn(_time.GetUtcNow()).ShouldBeFalse();
    }

    [Fact]
    public void CanSignIn_JustBeforeLockoutEnds_ReturnsFalse()
    {
        var user = new UserBuilder().Build();
        FailLogins(user, MaxAttempts);

        _time.Advance(_lockoutDuration - TimeSpan.FromSeconds(1));

        user.CanSignIn(_time.GetUtcNow()).ShouldBeFalse();
    }

    [Fact]
    public void CanSignIn_WhenLockoutEnds_ReturnsTrue()
    {
        var user = new UserBuilder().Build();
        FailLogins(user, MaxAttempts);

        _time.Advance(_lockoutDuration);

        user.CanSignIn(_time.GetUtcNow()).ShouldBeTrue();
    }

    [Fact]
    public void RegisterFailedLogin_AfterLockoutExpires_StartsCountingAgain()
    {
        var user = new UserBuilder().Build();
        FailLogins(user, MaxAttempts);
        _time.Advance(_lockoutDuration);

        FailLogins(user, 1);

        user.FailedLoginAttempts.ShouldBe(1);
        user.CanSignIn(_time.GetUtcNow()).ShouldBeTrue();
    }

    [Fact]
    public void RegisterSuccessfulLogin_AfterFailuresAndLockout_ClearsThemAndRecordsLogin()
    {
        var user = new UserBuilder().Build();
        FailLogins(user, MaxAttempts + 2);
        _time.Advance(_lockoutDuration);

        user.RegisterSuccessfulLogin(_time.GetUtcNow());

        user.FailedLoginAttempts.ShouldBe(0);
        user.LockoutEndsAt.ShouldBeNull();
        user.LastLoginAt.ShouldBe(_time.GetUtcNow());
        user.UpdatedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void RegisterFailedLogin_NonPositiveMaxAttempts_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new UserBuilder().Build().RegisterFailedLogin(_time.GetUtcNow(), 0, _lockoutDuration));

    [Fact]
    public void RegisterFailedLogin_NonPositiveDuration_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new UserBuilder().Build().RegisterFailedLogin(_time.GetUtcNow(), MaxAttempts, TimeSpan.Zero));

    [Fact]
    public void ChangePassword_NewHash_ReplacesHashAndRecordsChange()
    {
        var user = new UserBuilder().Build();
        _time.Advance(TimeSpan.FromDays(1));

        user.ChangePassword("new-hash", _time.GetUtcNow());

        user.PasswordHash.ShouldBe("new-hash");
        user.PasswordChangedAt.ShouldBe(_time.GetUtcNow());
        user.UpdatedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void ChangePassword_BlankHash_Throws() =>
        Should.Throw<ArgumentException>(() => new UserBuilder().Build().ChangePassword("", _time.GetUtcNow()));

    [Fact]
    public void ChangeRole_NewRole_UpdatesRole()
    {
        var user = new UserBuilder().WithRole(UserRole.Viewer).Build();
        _time.Advance(TimeSpan.FromHours(1));

        user.ChangeRole(UserRole.Admin, _time.GetUtcNow());

        user.Role.ShouldBe(UserRole.Admin);
        user.UpdatedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void ChangeRole_UndefinedRole_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new UserBuilder().Build().ChangeRole((UserRole)0, _time.GetUtcNow()));

    [Fact]
    public void Deactivate_ActiveUser_BlocksSignIn()
    {
        var user = new UserBuilder().Build();

        user.Deactivate(_time.GetUtcNow());

        user.Status.ShouldBe(UserStatus.Inactive);
        user.IsActive.ShouldBeFalse();
        user.CanSignIn(_time.GetUtcNow()).ShouldBeFalse();
    }

    [Fact]
    public void Activate_InactiveUser_AllowsSignInAgain()
    {
        var user = new UserBuilder().Inactive().Build();

        user.Activate(_time.GetUtcNow());

        user.Status.ShouldBe(UserStatus.Active);
        user.CanSignIn(_time.GetUtcNow()).ShouldBeTrue();
    }

    private void FailLogins(User user, int times)
    {
        for (var attempt = 0; attempt < times; attempt++)
        {
            user.RegisterFailedLogin(_time.GetUtcNow(), MaxAttempts, _lockoutDuration);
        }
    }
}
