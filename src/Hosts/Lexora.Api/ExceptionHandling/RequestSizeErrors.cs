using Lexora.Shared.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lexora.Api.ExceptionHandling;

public static class RequestSizeErrors
{
    public static Notification TooLarge { get; } = Notification.Validation("request.tooLarge", "The request is larger than the allowed size.");

    public static bool IsTooLarge(ModelError error) => IsTooLarge(error.Exception) || MentionsSizeLimit(error.ErrorMessage);

    public static bool IsTooLarge(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }
                || (current is InvalidDataException && MentionsSizeLimit(current.Message)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MentionsSizeLimit(string? message) =>
        message is not null
        && (message.Contains("length limit", StringComparison.OrdinalIgnoreCase) || message.Contains("body too large", StringComparison.OrdinalIgnoreCase));
}
