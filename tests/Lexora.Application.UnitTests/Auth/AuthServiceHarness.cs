using Lexora.Application.Auth.Login;
using Lexora.Application.Auth.Services;
using Lexora.Application.Common.Options;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Application.UnitTests.Auth;

internal sealed class AuthServiceHarness
{
    public const string Password = "Str0ngPassword";

    public AuthServiceHarness()
    {
        Service = new AuthService(
            Users,
            RefreshTokens,
            Hasher,
            new FakeAccessTokenIssuer(Time),
            new FakeSecretTokenGenerator(),
            CurrentUser,
            Options.Create(new AuthOptions()),
            Time,
            NullLogger<AuthService>.Instance);
    }

    public FakeTimeProvider Time { get; } = new(TestClock.Now);

    public FakePasswordHasher Hasher { get; } = new();

    public InMemoryUserRepository Users { get; } = new();

    public InMemoryRefreshTokenRepository RefreshTokens { get; } = new();

    public FakeCurrentUser CurrentUser { get; } = new();

    public AuthService Service { get; }

    public async Task<User> AddUserAsync(string email = "ana.souza@lexora.local")
    {
        var user = new UserBuilder().WithEmail(email).WithPasswordHash(Hasher.Hash(Password)).Build();
        await Users.AddAsync(user, CancellationToken.None);

        return user;
    }

    public async Task<LoginResponse> SignInAsync(User user)
    {
        var login = await Service.LoginAsync(new LoginCommand(user.Email.Value, Password), TestContext.Current.CancellationToken);
        CurrentUser.SignIn(user);

        return login.Value;
    }

    public RefreshToken TokenFor(string refreshToken) => RefreshTokens.Tokens.Single(token => token.TokenHash == $"sha256::{refreshToken}");
}
