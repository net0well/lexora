using Lexora.Shared.Results;

namespace Lexora.Application.Users;

public static class UserNotifications
{
    public const string InvalidRoleKey = "user.role.invalid";

    public static Notification NotFound { get; } = Notification.NotFound("user.notFound", "User not found.");

    public static Notification EmailDuplicate { get; } =
        Notification.Conflict("user.email.duplicate", "A user with this e-mail already exists.");

    public static Notification LastAdmin { get; } =
        Notification.BusinessRule("user.lastAdmin", "The last active administrator cannot be demoted or deactivated.");
}
