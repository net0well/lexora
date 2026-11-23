namespace Lexora.Infrastructure.AI.Providers;

public static class ProviderHttpClient
{
    // One long-lived client per profile; recycling pooled connections keeps DNS changes visible.
    public static HttpClient Create(Uri? baseAddress, TimeSpan timeout) =>
        new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
        {
            BaseAddress = baseAddress,
            Timeout = timeout,
        };
}
