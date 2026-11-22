using Lexora.Application.Auth;
using Lexora.Application.Auth.Login;
using Lexora.Application.Auth.Services;
using Lexora.Application.Common.Options;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Lexora.Application.UnitTests.Auth;

public sealed class AuthServiceLoginTests
{
    private const string Password = "Str0ngPassword";

    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly FakePasswordHasher _hasher = new();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly AuthOptions _options = new();

    [Fact]
    public async Task LoginAsync_ValidCredentials_IssuesTokensAndRecordsTheLogin()
    {
        var user = Existing(UserRole.Admin);

        var result = await Login(user.Email.Value);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe($"access-token:{user.Id}");
        result.Value.ExpiresAt.ShouldBe(TestClock.Now + FakeAccessTokenIssuer.Lifetime);
        result.Value.User.ShouldBe(new AuthenticatedUserResponse(user.Id, user.Email.Value, user.Name.Value, UserRole.Admin));
        result.Value.RefreshToken.ShouldBe("refresh-token-1");
        result.Value.RefreshTokenExpiresAt.ShouldBe(TestClock.Now + _options.RefreshTokenLifetime);
        user.LastLoginAt.ShouldBe(TestClock.Now);
        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<RefreshToken>(token => token.UserId == user.Id && token.TokenHash == "sha256::refresh-token-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_FailsAsInvalidCredentialsWithoutIssuingTokens()
    {
        var result = await Login("nobody@lexora.local");

        result.Notifications.ShouldBe([AuthNotifications.InvalidCredentials]);
        await _refreshTokens.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_MalformedEmail_FailsWithoutQueryingUsers()
    {
        var result = await Login("not-an-email");

        result.Notifications.ShouldBe([AuthNotifications.InvalidCredentials]);
        await _users.DidNotReceive().GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_CountsTheFailedAttempt()
    {
        var user = Existing();

        var result = await Login(user.Email.Value, "WrongPassw0rd");

        result.Notifications.ShouldBe([AuthNotifications.InvalidCredentials]);
        user.FailedLoginAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task LoginAsync_WrongPasswordAtTheLimit_LocksTheAccount()
    {
        var user = Existing();

        for (var attempt = 1; attempt < _options.Lockout.MaxFailedAttempts; attempt++)
        {
            await Login(user.Email.Value, "WrongPassw0rd");
        }

        var result = await Login(user.Email.Value, "WrongPassw0rd");

        result.Notifications.ShouldBe([AuthNotifications.LockedOut]);
        user.IsLockedOut(TestClock.Now).ShouldBeTrue();
    }

    [Fact]
    public async Task LoginAsync_LockedAccountWithTheRightPassword_FailsAsLockedOut()
    {
        var user = LockedOut();

        (await Login(user.Email.Value)).Notifications.ShouldBe([AuthNotifications.LockedOut]);
    }

    [Fact]
    public async Task LoginAsync_AfterTheLockoutEnds_Succeeds()
    {
        var user = LockedOut();
        _time.Advance(_options.Lockout.Duration);

        (await Login(user.Email.Value)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_FailsWithTheSameAnswerAsAWrongPassword()
    {
        var user = new UserBuilder().WithPasswordHash(_hasher.Hash(Password)).Inactive().Build();
        _users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        (await Login(user.Email.Value)).Notifications.ShouldBe([AuthNotifications.InvalidCredentials]);
    }

    [Fact]
    public async Task LoginAsync_PasswordStoredInAnOlderFormat_IsRehashed()
    {
        var user = new UserBuilder().WithPasswordHash(FakePasswordHasher.LegacyHashOf(Password)).Build();
        _users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        (await Login(user.Email.Value)).IsSuccess.ShouldBeTrue();

        user.PasswordHash.ShouldBe(_hasher.Hash(Password));
    }

    private User Existing(UserRole role = UserRole.Member)
    {
        var user = new UserBuilder().WithRole(role).WithPasswordHash(_hasher.Hash(Password)).Build();
        _users.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        return user;
    }

    private User LockedOut()
    {
        var user = Existing();
        user.RegisterFailedLogin(TestClock.Now, 1, _options.Lockout.Duration);

        return user;
    }

    private Task<Result<LoginResponse>> Login(string email, string password = Password) =>
        new AuthService(
            _users,
            _refreshTokens,
            _hasher,
            new FakeAccessTokenIssuer(_time),
            new FakeSecretTokenGenerator(),
            new FakeCurrentUser(),
            Options.Create(_options),
            _time,
            NullLogger<AuthService>.Instance)
        .LoginAsync(new LoginCommand(email, password), TestContext.Current.CancellationToken);
}
