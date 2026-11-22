using Lexora.Application.Common.Paging;
using Lexora.Application.Users;
using Lexora.Application.Users.Abstractions;
using Lexora.Application.Users.GetUser;
using Lexora.Application.Users.ListUsers;
using Lexora.Domain.Users;
using Lexora.TestKit;
using NSubstitute;

namespace Lexora.Application.UnitTests.Users;

public sealed class UserQueryHandlerTests
{
    private static readonly UserResponse _user = new(
        Guid.CreateVersion7(), "ana@lexora.local", "Ana", UserRole.Member, UserStatus.Active, null, TestClock.Now, TestClock.Now);

    private readonly IUserQueries _queries = Substitute.For<IUserQueries>();

    [Fact]
    public async Task GetUser_ExistingUser_ReturnsIt()
    {
        _queries.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);

        (await GetUserHandler.Handle(new GetUserQuery(_user.Id), _queries, TestContext.Current.CancellationToken)).Value.ShouldBe(_user);
    }

    [Fact]
    public async Task GetUser_UnknownUser_FailsAsNotFound() =>
        (await GetUserHandler.Handle(new GetUserQuery(Guid.CreateVersion7()), _queries, TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([UserNotifications.NotFound]);

    [Fact]
    public async Task ListUsers_Query_ReturnsThePageFromTheQueries()
    {
        var query = new ListUsersQuery("ana", UserRole.Member, null);
        var page = PagedResponse.Create<UserResponse>([_user], query.Paging, totalItems: 1);
        _queries.ListAsync(query, Arg.Any<CancellationToken>()).Returns(page);

        (await ListUsersHandler.Handle(query, _queries, TestContext.Current.CancellationToken)).Value.ShouldBe(page);
    }
}
