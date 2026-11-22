using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Lexora.Api.RateLimiting;

public static class ClientIpRateLimiter
{
    private const string UnknownClient = "unknown";

    public static RateLimitPartition<string> Partition(HttpContext context, Func<AuthRateLimitOptions, int> permitsPerMinute)
    {
        var limits = context.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        var client = context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient;

        return RateLimitPartition.GetFixedWindowLimiter(client, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitsPerMinute(limits),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }
}
