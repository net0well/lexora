using Lexora.Application.Common.Paging;
using Lexora.Application.Users;
using Lexora.Application.Users.ListUsers;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence.Queries;
using Lexora.TestKit.Builders;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class UserQueriesTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task GetByIdAsync_ExistingUser_ProjectsTheResponse()
    {
        var user = new UserBuilder().WithEmail("ana.souza@lexora.local").WithName("Ana Souza").WithRole(UserRole.Admin).Build();
        await SaveAsync(user);

        await using var context = Database.CreateContext();
        var response = await new UserQueries(context).GetByIdAsync(user.Id, CancellationToken);

        response.ShouldNotBeNull();
        response.Email.ShouldBe("ana.souza@lexora.local");
        response.Name.ShouldBe("Ana Souza");
        response.Role.ShouldBe(UserRole.Admin);
        response.Status.ShouldBe(UserStatus.Active);
        response.CreatedAt.ShouldBe(user.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownUser_ReturnsNull()
    {
        await using var context = Database.CreateContext();

        (await new UserQueries(context).GetByIdAsync(Guid.CreateVersion7(), CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_NoFilters_PagesOrderedByName()
    {
        await SaveAsync(
            new UserBuilder().WithEmail("c@lexora.local").WithName("Carla").Build(),
            new UserBuilder().WithEmail("a@lexora.local").WithName("Ana").Build(),
            new UserBuilder().WithEmail("b@lexora.local").WithName("Bruno").Build());

        var page = await ListAsync(new ListUsersQuery(null, null, null, Page: 2, PageSize: 2));

        page.Items.Select(user => user.Name).ShouldBe(["Carla"]);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(2);
        page.TotalItems.ShouldBe(3);
        page.TotalPages.ShouldBe(2);
    }

    [Theory]
    [InlineData("SOUZA", "ana.souza@lexora.local")]
    [InlineData("bruno@", "bruno@lexora.local")]
    public async Task ListAsync_Search_MatchesNameOrEmailIgnoringCase(string search, string expectedEmail)
    {
        await SaveAsync(
            new UserBuilder().WithEmail("ana.souza@lexora.local").WithName("Ana Souza").Build(),
            new UserBuilder().WithEmail("bruno@lexora.local").WithName("Bruno Lima").Build());

        var page = await ListAsync(new ListUsersQuery(search, null, null));

        page.Items.ShouldHaveSingleItem().Email.ShouldBe(expectedEmail);
    }

    [Fact]
    public async Task ListAsync_SearchWithWildcards_TreatsThemAsLiterals()
    {
        await SaveAsync(new UserBuilder().WithEmail("ana@lexora.local").WithName("Ana").Build());

        (await ListAsync(new ListUsersQuery("%", null, null))).TotalItems.ShouldBe(0);
        (await ListAsync(new ListUsersQuery("_", null, null))).TotalItems.ShouldBe(0);
    }

    [Fact]
    public async Task ListAsync_RoleAndStatus_FilterTogether()
    {
        await SaveAsync(
            new UserBuilder().WithEmail("admin@lexora.local").WithRole(UserRole.Admin).Build(),
            new UserBuilder().WithEmail("old.admin@lexora.local").WithRole(UserRole.Admin).Inactive().Build(),
            new UserBuilder().WithEmail("member@lexora.local").WithRole(UserRole.Member).Build());

        var page = await ListAsync(new ListUsersQuery(null, UserRole.Admin, UserStatus.Inactive));

        page.Items.ShouldHaveSingleItem().Email.ShouldBe("old.admin@lexora.local");
    }

    private async Task<PagedResponse<UserResponse>> ListAsync(ListUsersQuery query)
    {
        await using var context = Database.CreateContext();

        return await new UserQueries(context).ListAsync(query, CancellationToken);
    }
}
