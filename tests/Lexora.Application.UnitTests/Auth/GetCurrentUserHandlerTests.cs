using Lexora.Application.Auth;
using Lexora.Application.Auth.GetCurrentUser;
using Lexora.Domain.Users;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;

namespace Lexora.Application.UnitTests.Auth;

public sealed class GetCurrentUserHandlerTests
{
    private readonly InMemoryUserRepository _users = new();

    [Fact]
    public async Task Handle_AuthenticatedUser_ReturnsTheirProfile()
    {
        var user = new UserBuilder().WithRole(UserRole.Viewer).Build();
        await _users.AddAsync(user, TestContext.Current.CancellationToken);

        var result = await GetCurrentUserHandler.Handle(
            new GetCurrentUserQuery(),
            new FakeCurrentUser().SignIn(user),
            _users,
            TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new AuthenticatedUserResponse(user.Id, user.Email.Value, user.Name.Value, UserRole.Viewer));
    }

    [Fact]
    public async Task Handle_UserNoLongerExists_FailsAsNotFound()
    {
        var result = await GetCurrentUserHandler.Handle(
            new GetCurrentUserQuery(),
            new FakeCurrentUser().SignIn(new UserBuilder().Build()),
            _users,
            TestContext.Current.CancellationToken);

        result.Notifications.Single().Key.ShouldBe("user.notFound");
    }
}
