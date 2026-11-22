using FluentValidation.Results;
using Lexora.Application.Common.Validation;
using Lexora.Infrastructure.Messaging.Validation;
using Lexora.Shared.Results;

namespace Lexora.Infrastructure.IntegrationTests.Messaging;

public sealed class NotificationFailureActionTests
{
    [Fact]
    public void Throw_ValidationFailures_RaisesInputValidationExceptionWithNotifications()
    {
        ValidationFailure[] failures =
        [
            new("Name", "The name is required.") { ErrorCode = "knowledgeBase.name.required" },
            new("Description", "The description is too long."),
        ];

        var exception = Should.Throw<InputValidationException>(() => new NotificationFailureAction<object>().Throw(new object(), failures));

        exception.Notifications.ShouldBe(
        [
            Notification.Validation("knowledgeBase.name.required", "The name is required."),
            Notification.Validation("Description", "The description is too long."),
        ]);
    }
}
