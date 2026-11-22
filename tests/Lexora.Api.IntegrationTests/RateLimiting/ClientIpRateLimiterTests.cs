using System.Net;
using Lexora.Api.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lexora.Api.IntegrationTests.RateLimiting;

public sealed class ClientIpRateLimiterTests
{
    [Fact]
    public void Partition_Request_IsKeyedByTheClientAddress()
    {
        var partition = ClientIpRateLimiter.Partition(NewContext("203.0.113.7"), limits => limits.LoginPermitsPerMinute);

        partition.PartitionKey.ShouldBe("203.0.113.7");
    }

    [Fact]
    public void Partition_RequestWithoutAddress_SharesTheUnknownBucket() =>
        ClientIpRateLimiter.Partition(NewContext(address: null), limits => limits.LoginPermitsPerMinute).PartitionKey.ShouldBe("unknown");

    [Fact]
    public void Partition_ConfiguredLimit_RejectsTheRequestAfterIt()
    {
        var partition = ClientIpRateLimiter.Partition(NewContext("203.0.113.7", loginPermits: 2), limits => limits.LoginPermitsPerMinute);
        using var limiter = partition.Factory(partition.PartitionKey);

        using var first = limiter.AttemptAcquire();
        using var second = limiter.AttemptAcquire();
        using var third = limiter.AttemptAcquire();

        first.IsAcquired.ShouldBeTrue();
        second.IsAcquired.ShouldBeTrue();
        third.IsAcquired.ShouldBeFalse();
    }

    private static DefaultHttpContext NewContext(string? address, int loginPermits = 10)
    {
        var services = new ServiceCollection()
            .AddSingleton(Options.Create(new AuthRateLimitOptions { LoginPermitsPerMinute = loginPermits }))
            .BuildServiceProvider();

        return new DefaultHttpContext
        {
            RequestServices = services,
            Connection = { RemoteIpAddress = address is null ? null : IPAddress.Parse(address) },
        };
    }
}
