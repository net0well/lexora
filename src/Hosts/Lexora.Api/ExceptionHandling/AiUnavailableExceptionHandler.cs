using System.Globalization;
using Lexora.Api.Contracts;
using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Ai;
using Microsoft.AspNetCore.Diagnostics;

namespace Lexora.Api.ExceptionHandling;

public sealed class AiUnavailableExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AiProviderUnavailableException unavailable || httpContext.Response.HasStarted)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        if (unavailable.RetryAfter is { } retryAfter)
        {
            httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await httpContext.Response.WriteAsJsonAsync(CommandResult.Fail([AiNotifications.Unavailable]), cancellationToken);

        return true;
    }
}
