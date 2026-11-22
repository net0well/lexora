using System.Diagnostics;
using Lexora.Api.Contracts;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Diagnostics;

namespace Lexora.Api.ExceptionHandling;

public sealed partial class UnhandledExceptionHandler(ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
{
    public const string Key = "unexpected";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        LogUnhandled(exception, traceId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            CommandResult.Fail([new Notification(Key, $"An unexpected error occurred. Trace id: {traceId}", NotificationType.BusinessRule)]),
            cancellationToken);

        return true;
    }

    [LoggerMessage(EventId = 5000, Level = LogLevel.Error, Message = "Unhandled exception for trace {TraceId}")]
    private partial void LogUnhandled(Exception exception, string traceId);
}
