using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence.Repositories;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class UserRepositoryTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task AddAsync_NewUser_RoundTripsEveryField()
    {
        var user = new UserBuilder().WithEmail("Ana.Souza@Lexora.Local").WithRole(UserRole.Admin).Build();
        user.RegisterFailedLogin(TestClock.Now, 5, TimeSpan.FromMinutes(15));
        user.RegisterSuccessfulLogin(TestClock.Now.AddMinutes(1));

        await using (var context = Database.CreateContext())
        {
            await new UserRepository(context).AddAsync(user, CancellationToken);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using var reader = Database.CreateContext();
        var loaded = await new UserRepository(reader).GetByIdAsync(user.Id, CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.Email.ShouldBe(user.Email);
        loaded.Name.ShouldBe(user.Name);
        loaded.PasswordHash.ShouldBe(user.PasswordHash);
        loaded.Role.ShouldBe(UserRole.Admin);
        loaded.Status.ShouldBe(UserStatus.Active);
        loaded.FailedLoginAttempts.ShouldBe(0);
        loaded.LockoutEndsAt.ShouldBeNull();
        loaded.LastLoginAt.ShouldBe(TestClock.Now.AddMinutes(1));
        loaded.PasswordChangedAt.ShouldBe(user.PasswordChangedAt);
        loaded.CreatedAt.ShouldBe(user.CreatedAt);
        loaded.UpdatedAt.ShouldBe(user.UpdatedAt);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        var user = await SaveUserAsync("ana.souza@lexora.local");

        await using var context = Database.CreateContext();
        var loaded = await new UserRepository(context).GetByEmailAsync(Email.Create("ANA.SOUZA@lexora.local").Value, CancellationToken);

        loaded.ShouldNotBeNull().Id.ShouldBe(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        await SaveUserAsync("ana.souza@lexora.local");

        await using var context = Database.CreateContext();

        (await new UserRepository(context).GetByEmailAsync(Email.Create("bruno@lexora.local").Value, CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ExistingAndUnknownEmails_ReportsEach()
    {
        await SaveUserAsync("ana.souza@lexora.local");

        await using var context = Database.CreateContext();
        var repository = new UserRepository(context);

        (await repository.ExistsByEmailAsync(Email.Create("ana.souza@lexora.local").Value, CancellationToken)).ShouldBeTrue();
        (await repository.ExistsByEmailAsync(Email.Create("bruno@lexora.local").Value, CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task CountActiveAdminsAsync_MixedUsers_CountsOnlyActiveAdmins()
    {
        var inactiveAdmin = new UserBuilder().WithEmail("old.admin@lexora.local").WithRole(UserRole.Admin).Inactive().Build();
        await SaveAsync(
            new UserBuilder().WithEmail("admin@lexora.local").WithRole(UserRole.Admin).Build(),
            inactiveAdmin,
            new UserBuilder().WithEmail("member@lexora.local").WithRole(UserRole.Member).Build());

        await using var context = Database.CreateContext();

        (await new UserRepository(context).CountActiveAdminsAsync(CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_DuplicateEmail_IsRejectedByTheDatabase()
    {
        await SaveUserAsync("ana.souza@lexora.local");

        await Should.ThrowAsync<DbUpdateException>(() => SaveUserAsync("ana.souza@lexora.local"));
    }
}
