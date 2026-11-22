using Lexora.Application.Auth.SeedAdmin;
using Lexora.Application.Auth.Services;
using Lexora.Domain.Users;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Application.UnitTests.Auth;

public sealed class SeedAdminServiceTests
{
    private const string Password = "Str0ngAdminPass";

    private readonly InMemoryUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly SeedAdminService _service;

    public SeedAdminServiceTests() =>
        _service = new SeedAdminService(_users, _hasher, new SeedAdminValidator(), new FakeTimeProvider(TestClock.Now));

    [Fact]
    public async Task SeedAsync_NoAdminYet_CreatesAnActiveAdmin()
    {
        var result = await SeedAsync("admin@lexora.local", "Administrator", Password);

        result.Value.Created.ShouldBeTrue();
        var admin = _users.Users.Single();
        admin.Id.ShouldBe(result.Value.UserId.ShouldNotBeNull());
        admin.Email.Value.ShouldBe("admin@lexora.local");
        admin.Role.ShouldBe(UserRole.Admin);
        admin.IsActive.ShouldBeTrue();
        admin.PasswordHash.ShouldBe(_hasher.Hash(Password));
    }

    [Fact]
    public async Task SeedAsync_AdminAlreadyExists_DoesNothing()
    {
        await _users.AddAsync(new UserBuilder().WithEmail("first@lexora.local").WithRole(UserRole.Admin).Build(), TestContext.Current.CancellationToken);

        var result = await SeedAsync("admin@lexora.local", "Administrator", Password);

        result.Value.Created.ShouldBeFalse();
        _users.Users.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SeedAsync_AdminAlreadyExistsAndPasswordMissing_StillSucceedsWithoutChanges()
    {
        await _users.AddAsync(new UserBuilder().WithRole(UserRole.Admin).Build(), TestContext.Current.CancellationToken);

        (await SeedAsync("admin@lexora.local", "Administrator", null)).Value.Created.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData("X1admin@lexora.local")]
    public async Task SeedAsync_MissingOrWeakPassword_FailsWithoutCreatingAnything(string? password)
    {
        var result = await SeedAsync("admin@lexora.local", "Administrator", password);

        result.Notifications.ShouldContain(notification => notification.Key == "auth.password.weak");
        _users.Users.ShouldBeEmpty();
    }

    [Fact]
    public async Task SeedAsync_EmailOfAnExistingMember_FailsAsDuplicate()
    {
        await _users.AddAsync(new UserBuilder().WithEmail("admin@lexora.local").WithRole(UserRole.Member).Build(), TestContext.Current.CancellationToken);

        (await SeedAsync("admin@lexora.local", "Administrator", Password)).Notifications.Single().Key.ShouldBe("user.email.duplicate");
    }

    [Fact]
    public async Task SeedAsync_MalformedEmail_FailsAsInvalidEmail() =>
        (await SeedAsync("not-an-email", "Administrator", Password)).Notifications.Single().Key.ShouldBe("user.email.invalid");

    private Task<Result<SeedAdminResponse>> SeedAsync(string? email, string? name, string? password) =>
        _service.SeedAsync(new SeedAdminCommand(email, name, password), TestContext.Current.CancellationToken);
}
