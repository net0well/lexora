using Lexora.Domain.Users;
using Lexora.TestKit.Builders;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public abstract class DatabaseTest(DatabaseFixture database) : IAsyncLifetime
{
    protected DatabaseFixture Database { get; } = database;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await Database.ResetAsync(CancellationToken);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }

    protected async Task SaveAsync(params IEnumerable<object> entities)
    {
        await using var context = Database.CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync(CancellationToken);
    }

    protected async Task<User> SaveUserAsync(string email = "ana.souza@lexora.local", UserRole role = UserRole.Member)
    {
        var user = new UserBuilder().WithEmail(email).WithRole(role).Build();
        await SaveAsync(user);

        return user;
    }
}
