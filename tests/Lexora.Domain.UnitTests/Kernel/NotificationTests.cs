using Lexora.Shared.Results;

namespace Lexora.Domain.UnitTests.Kernel;

public sealed class NotificationTests
{
    [Fact]
    public void Factories_EachKind_SetsMatchingType() =>
        new[]
        {
            Notification.Validation("k", "m"),
            Notification.NotFound("k", "m"),
            Notification.Conflict("k", "m"),
            Notification.Unauthorized("k", "m"),
            Notification.Forbidden("k", "m"),
            Notification.BusinessRule("k", "m"),
        }
        .Select(notification => notification.Type)
        .ShouldBe(
        [
            NotificationType.Validation,
            NotificationType.NotFound,
            NotificationType.Conflict,
            NotificationType.Unauthorized,
            NotificationType.Forbidden,
            NotificationType.BusinessRule,
        ]);

    [Fact]
    public void Factory_KeyAndMessage_AreKept()
    {
        var notification = Notification.Validation("user.email.invalid", "The e-mail address is invalid.");

        notification.Key.ShouldBe("user.email.invalid");
        notification.Message.ShouldBe("The e-mail address is invalid.");
    }

    [Fact]
    public void Equals_SameKeyMessageAndType_ReturnsTrue() =>
        Notification.Conflict("document.duplicate", "Duplicate.").ShouldBe(Notification.Conflict("document.duplicate", "Duplicate."));
}
