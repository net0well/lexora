using Lexora.Shared.Results;

namespace Lexora.Api.Results;

public static class NotificationStatusCodeMapper
{
    public static int ToStatusCode(IReadOnlyList<Notification> notifications) => (notifications.Count > 0 ? notifications[0].Type : (NotificationType?)null) switch
    {
        null => StatusCodes.Status200OK,
        NotificationType.Validation => StatusCodes.Status400BadRequest,
        NotificationType.Unauthorized => StatusCodes.Status401Unauthorized,
        NotificationType.Forbidden => StatusCodes.Status403Forbidden,
        NotificationType.NotFound => StatusCodes.Status404NotFound,
        NotificationType.Conflict => StatusCodes.Status409Conflict,
        NotificationType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };
}
