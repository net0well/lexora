using Lexora.Api.Results;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Http;

namespace Lexora.Api.IntegrationTests.Results;

public sealed class NotificationStatusCodeMapperTests
{
    [Theory]
    [InlineData(NotificationType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(NotificationType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(NotificationType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(NotificationType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(NotificationType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(NotificationType.BusinessRule, StatusCodes.Status422UnprocessableEntity)]
    public void ToStatusCode_NotificationType_MapsToTheContractStatus(NotificationType type, int expected) =>
        NotificationStatusCodeMapper.ToStatusCode([new Notification("key", "message", type)]).ShouldBe(expected);

    [Fact]
    public void ToStatusCode_NoNotifications_IsOk() => NotificationStatusCodeMapper.ToStatusCode([]).ShouldBe(StatusCodes.Status200OK);

    [Fact]
    public void ToStatusCode_SeveralNotifications_UsesTheFirstOne() =>
        NotificationStatusCodeMapper.ToStatusCode([Notification.Conflict("a", "a"), Notification.Validation("b", "b")])
            .ShouldBe(StatusCodes.Status409Conflict);
}
