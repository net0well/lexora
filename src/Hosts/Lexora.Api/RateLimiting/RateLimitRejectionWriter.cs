using System.Globalization;
using System.Threading.RateLimiting;
using Lexora.Api.Contracts;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.RateLimiting;

namespace Lexora.Api.RateLimiting;

public static class RateLimitRejectionWriter
{
    public const string Key = "request.rateLimited";

    public static async ValueTask WriteAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        await response.WriteAsJsonAsync(
            CommandResult.Fail([new Notification(Key, "Too many requests. Try again later.", NotificationType.BusinessRule)]),
            cancellationToken);
    }
}
