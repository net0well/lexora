using Lexora.Api.Contracts;
using Microsoft.AspNetCore.Diagnostics;

namespace Lexora.Api.ExceptionHandling;

public sealed class RequestTooLargeExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!RequestSizeErrors.IsTooLarge(exception) || httpContext.Response.HasStarted)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await httpContext.Response.WriteAsJsonAsync(CommandResult.Fail([RequestSizeErrors.TooLarge]), cancellationToken);

        return true;
    }
}
