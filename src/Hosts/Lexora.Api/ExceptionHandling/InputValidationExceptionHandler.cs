using Lexora.Api.Contracts;
using Lexora.Application.Common.Validation;
using Microsoft.AspNetCore.Diagnostics;

namespace Lexora.Api.ExceptionHandling;

public sealed class InputValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not InputValidationException validation)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(CommandResult.Fail(validation.Notifications), cancellationToken);

        return true;
    }
}
