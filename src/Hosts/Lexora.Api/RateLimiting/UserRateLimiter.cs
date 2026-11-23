using System.Threading.RateLimiting;
using Lexora.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace Lexora.Api.RateLimiting;

public static class UserRateLimiter
{
    public static RateLimitPartition<string> Partition(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var limits = context.RequestServices.GetRequiredService<IOptions<ChatRateLimitOptions>>().Value;

        // Authentication runs first, so the user id is known; the address only covers requests that will be refused anyway.
        var caller = context.User.FindFirst(ClaimNames.Subject)?.Value is { Length: > 0 } userId
            ? $"user:{userId}"
            : $"ip:{context.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(caller, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limits.PermitsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }
}
