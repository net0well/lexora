using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Lexora.Api.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace Lexora.Api.IntegrationTests.RateLimiting;

public sealed class RateLimitRejectionWriterTests
{
    [Fact]
    public async Task WriteAsync_RejectedLease_RespondsTooManyRequestsWithRetryAfterAndEnvelope()
    {
        using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(30),
            QueueLimit = 0,
        });
        using var granted = limiter.AttemptAcquire();
        using var rejected = limiter.AttemptAcquire();
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await RateLimitRejectionWriter.WriteAsync(
            new OnRejectedContext { HttpContext = httpContext, Lease = rejected },
            TestContext.Current.CancellationToken);

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        int.Parse(httpContext.Response.Headers.RetryAfter.ToString(), CultureInfo.InvariantCulture).ShouldBeInRange(1, 30);
        httpContext.Response.Body.Position = 0;
        var body = (await JsonDocument.ParseAsync(httpContext.Response.Body, cancellationToken: TestContext.Current.CancellationToken)).RootElement;
        body.GetProperty("success").GetBoolean().ShouldBeFalse();
        body.GetProperty("notifications")[0].GetProperty("key").GetString().ShouldBe(RateLimitRejectionWriter.Key);
    }
}
