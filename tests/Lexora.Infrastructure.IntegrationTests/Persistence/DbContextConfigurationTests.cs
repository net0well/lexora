namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class DbContextConfigurationTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task Configure_ExecutionStrategy_LeavesRetriesToTheMessagingPolicies()
    {
        await using var context = Database.CreateContext();

        context.Database.CreateExecutionStrategy().RetriesOnFailure.ShouldBeFalse();
    }
}
