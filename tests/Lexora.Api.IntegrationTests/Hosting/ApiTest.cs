namespace Lexora.Api.IntegrationTests.Hosting;

public abstract class ApiTest(ApiFactory api) : IAsyncLifetime
{
    protected ApiFactory Api { get; } = api;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await Api.ResetDatabaseAsync(CancellationToken);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }
}
