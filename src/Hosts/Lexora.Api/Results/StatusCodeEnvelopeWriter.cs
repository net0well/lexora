using Lexora.Api.Contracts;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Diagnostics;

namespace Lexora.Api.Results;

public static class StatusCodeEnvelopeWriter
{
    public static Task WriteAsync(StatusCodeContext context)
    {
        var response = context.HttpContext.Response;

        var notification = response.StatusCode switch
        {
            StatusCodes.Status404NotFound => Notification.NotFound("request.notFound", "The requested resource was not found."),
            StatusCodes.Status405MethodNotAllowed => Notification.Validation("request.methodNotAllowed", "The HTTP method is not allowed for this resource."),
            StatusCodes.Status415UnsupportedMediaType => Notification.Validation("request.unsupportedMediaType", "The content type is not supported."),
            _ => Notification.Validation("request.failed", "The request could not be processed."),
        };

        return response.WriteAsJsonAsync(CommandResult.Fail([notification]), context.HttpContext.RequestAborted);
    }
}
