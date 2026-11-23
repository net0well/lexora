using Lexora.Shared.Results;

namespace Lexora.Api.Results;

public static class NotificationStatusCodeMapper
{
    private const string FileTooLargeKey = "document.size.exceeded";

    public static int ToStatusCode(IReadOnlyList<Notification> notifications)
    {
        if (notifications.Count == 0)
        {
            return StatusCodes.Status200OK;
        }

        if (notifications[0].Key == FileTooLargeKey)
        {
            return StatusCodes.Status413PayloadTooLarge;
        }

        return notifications[0].Type switch
        {
            NotificationType.Validation => StatusCodes.Status400BadRequest,
            NotificationType.Unauthorized => StatusCodes.Status401Unauthorized,
            NotificationType.Forbidden => StatusCodes.Status403Forbidden,
            NotificationType.NotFound => StatusCodes.Status404NotFound,
            NotificationType.Conflict => StatusCodes.Status409Conflict,
            NotificationType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            NotificationType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
