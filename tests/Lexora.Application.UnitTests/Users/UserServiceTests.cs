using Lexora.Application.Users;
using Lexora.Application.Users.ActivateUser;
using Lexora.Application.Users.ChangeUserRole;
using Lexora.Application.Users.CreateUser;
using Lexora.Application.Users.DeactivateUser;
using Lexora.Application.Users.ResetUserPassword;
using Lexora.Application.Users.Services;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Application.UnitTests.Users;

public sealed class UserServiceTests
{
    private const string Password = "Str0ngPassword";

    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly FakePasswordHasher _hasher = new();
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();
    private readonly FakeCurrentUser _currentUser = new FakeCurrentUser().SignIn(new UserBuilder().WithEmail("caller@lexora.local").WithRole(UserRole.Admin).Build());
    private readonly UserService _service;

    public UserServiceTests() => _service = new UserService(_users, _refreshTokens, _hasher, _currentUser, _time);

    [Fact]
    public async Task CreateAsync_NewEmail_CreatesAnActiveUserWithTheHashedPassword()
    {
        var result = await _service.CreateAsync(
            new CreateUserCommand("Bruno.Lima@Lexora.local", "Bruno Lima", UserRole.Member, Password),
            TestContext.Current.CancellationToken);

        result.Value.Email.ShouldBe("bruno.lima@lexora.local");
        result.Value.Role.ShouldBe(UserRole.Member);
        result.Value.Status.ShouldBe(UserStatus.Active);
        _users.Users.Single().PasswordHash.ShouldBe(_hasher.Hash(Password));
    }

    [Fact]
    public async Task CreateAsync_EmailAlreadyUsed_FailsAsDuplicate()
    {
        await AddUserAsync("bruno@lexora.local", UserRole.Viewer);

        var result = await _service.CreateAsync(
            new CreateUserCommand("BRUNO@lexora.local", "Bruno", UserRole.Member, Password),
            TestContext.Current.CancellationToken);

        result.Notifications.ShouldBe([UserNotifications.EmailDuplicate]);
        _users.Users.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_NameTooShort_ReturnsTheDomainNotification()
    {
        var result = await _service.CreateAsync(new CreateUserCommand("x@lexora.local", "X", UserRole.Member, Password), TestContext.Current.CancellationToken);

        result.Notifications.Single().Key.ShouldBe("user.name.tooShort");
    }

    [Fact]
    public async Task ChangeRoleAsync_OneOfSeveralAdmins_DemotesIt()
    {
        var admin = await AddUserAsync("first@lexora.local", UserRole.Admin);
        await AddUserAsync("second@lexora.local", UserRole.Admin);

        var result = await _service.ChangeRoleAsync(new ChangeUserRoleCommand(admin.Id, UserRole.Viewer), TestContext.Current.CancellationToken);

        result.Value.Role.ShouldBe(UserRole.Viewer);
        admin.UpdatedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public async Task ChangeRoleAsync_LastActiveAdmin_KeepsItAdmin()
    {
        var admin = await AddUserAsync("only@lexora.local", UserRole.Admin);
        var inactiveAdmin = await AddUserAsync("inactive@lexora.local", UserRole.Admin);
        inactiveAdmin.Deactivate(_time.GetUtcNow());

        var result = await _service.ChangeRoleAsync(new ChangeUserRoleCommand(admin.Id, UserRole.Member), TestContext.Current.CancellationToken);

        result.Notifications.ShouldBe([UserNotifications.LastAdmin]);
        admin.Role.ShouldBe(UserRole.Admin);
    }

    [Fact]
    public async Task ChangeRoleAsync_LastAdminKeepingAdminRole_Succeeds()
    {
        var admin = await AddUserAsync("only@lexora.local", UserRole.Admin);

        (await _service.ChangeRoleAsync(new ChangeUserRoleCommand(admin.Id, UserRole.Admin), TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeRoleAsync_UnknownUser_FailsAsNotFound() =>
        (await _service.ChangeRoleAsync(new ChangeUserRoleCommand(Guid.CreateVersion7(), UserRole.Member), TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([UserNotifications.NotFound]);

    [Fact]
    public async Task DeactivateAsync_Member_DeactivatesAndRevokesEverySession()
    {
        var member = await AddUserAsync("member@lexora.local", UserRole.Member);
        var first = await IssueSessionAsync(member);
        var second = await IssueSessionAsync(member);

        var result = await _service.DeactivateAsync(new DeactivateUserCommand(member.Id), TestContext.Current.CancellationToken);

        result.Value.Status.ShouldBe(UserStatus.Inactive);
        first.RevocationReason.ShouldBe(RefreshTokenRevocationReason.UserDeactivated);
        second.RevocationReason.ShouldBe(RefreshTokenRevocationReason.UserDeactivated);
    }

    [Fact]
    public async Task DeactivateAsync_LastActiveAdmin_KeepsItActive()
    {
        var admin = await AddUserAsync("only@lexora.local", UserRole.Admin);
        var session = await IssueSessionAsync(admin);

        var result = await _service.DeactivateAsync(new DeactivateUserCommand(admin.Id), TestContext.Current.CancellationToken);

        result.Notifications.ShouldBe([UserNotifications.LastAdmin]);
        admin.IsActive.ShouldBeTrue();
        session.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task DeactivateAsync_OwnAccount_IsRejectedEvenWithOtherAdmins()
    {
        await AddUserAsync("other.admin@lexora.local", UserRole.Admin);
        var self = new UserBuilder().WithEmail("self@lexora.local").WithRole(UserRole.Admin).Build();
        await _users.AddAsync(self, TestContext.Current.CancellationToken);
        _currentUser.SignIn(self);

        var result = await _service.DeactivateAsync(new DeactivateUserCommand(self.Id), TestContext.Current.CancellationToken);

        result.Notifications.ShouldBe([UserNotifications.SelfDeactivation]);
        self.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_AlreadyInactiveAdmin_SucceedsWithoutCountingIt()
    {
        await AddUserAsync("active@lexora.local", UserRole.Admin);
        var inactive = await AddUserAsync("inactive@lexora.local", UserRole.Admin);
        inactive.Deactivate(_time.GetUtcNow());

        (await _service.DeactivateAsync(new DeactivateUserCommand(inactive.Id), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ActivateAsync_InactiveUser_ActivatesIt()
    {
        var user = await AddUserAsync("member@lexora.local", UserRole.Member);
        user.Deactivate(_time.GetUtcNow());

        var result = await _service.ActivateAsync(new ActivateUserCommand(user.Id), TestContext.Current.CancellationToken);

        result.Value.Status.ShouldBe(UserStatus.Active);
    }

    [Fact]
    public async Task ActivateAsync_UnknownUser_FailsAsNotFound() =>
        (await _service.ActivateAsync(new ActivateUserCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([UserNotifications.NotFound]);

    [Fact]
    public async Task ResetPasswordAsync_StrongPassword_ReplacesItAndRevokesEverySession()
    {
        var user = await AddUserAsync("member@lexora.local", UserRole.Member);
        var session = await IssueSessionAsync(user);

        var result = await _service.ResetPasswordAsync(new ResetUserPasswordCommand(user.Id, "An0therStrongOne"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        user.PasswordHash.ShouldBe(_hasher.Hash("An0therStrongOne"));
        session.RevocationReason.ShouldBe(RefreshTokenRevocationReason.PasswordChanged);
    }

    [Fact]
    public async Task ResetPasswordAsync_PasswordContainingTheEmail_FailsAsWeak()
    {
        var user = await AddUserAsync("ana@lexora.local", UserRole.Member);

        var result = await _service.ResetPasswordAsync(new ResetUserPasswordCommand(user.Id, "X1ana@lexora.local"), TestContext.Current.CancellationToken);

        result.Notifications.Single().Key.ShouldBe("auth.password.weak");
        user.PasswordHash.ShouldBe(_hasher.Hash(Password));
    }

    private async Task<User> AddUserAsync(string email, UserRole role)
    {
        var user = new UserBuilder().WithEmail(email).WithRole(role).WithPasswordHash(_hasher.Hash(Password)).Build();
        await _users.AddAsync(user, TestContext.Current.CancellationToken);

        return user;
    }

    private async Task<RefreshToken> IssueSessionAsync(User user)
    {
        var token = RefreshToken.Issue(user.Id, $"sha256::{Guid.CreateVersion7()}", _time.GetUtcNow().AddDays(7), _time.GetUtcNow());
        await _refreshTokens.AddAsync(token, TestContext.Current.CancellationToken);

        return token;
    }
}
