using Lexora.Api.Contracts;
using Lexora.Api.Results;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lexora.Api.IntegrationTests.Results;

public sealed class ResultActionExtensionsTests
{
    private static readonly Notification _duplicate = Notification.Conflict("knowledgeBase.name.duplicate", "Duplicate.");

    [Fact]
    public void ToActionResult_Success_ReturnsOkWithEmptyEnvelope()
    {
        var action = Result.Success().ToActionResult().ShouldBeOfType<OkObjectResult>();

        var envelope = action.Value.ShouldBeOfType<CommandResult>();
        envelope.Success.ShouldBeTrue();
        envelope.Data.ShouldBeNull();
        envelope.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public void ToActionResult_SuccessWithValue_ReturnsOkWithData()
    {
        var action = Result.Success(42).ToActionResult().ShouldBeOfType<OkObjectResult>();

        var envelope = action.Value.ShouldBeOfType<CommandResult<int>>();
        envelope.Success.ShouldBeTrue();
        envelope.Data.ShouldBe(42);
        envelope.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public void ToActionResult_Failure_ReturnsTheMappedStatusWithNotifications()
    {
        var action = Result.Failure<int>(_duplicate).ToActionResult().ShouldBeOfType<ObjectResult>();

        action.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        var envelope = action.Value.ShouldBeOfType<CommandResult>();
        envelope.Success.ShouldBeFalse();
        envelope.Data.ShouldBeNull();
        envelope.Notifications.ShouldBe([new NotificationResponse("knowledgeBase.name.duplicate", "Duplicate.", NotificationType.Conflict)]);
    }

    [Fact]
    public void ToCreatedResult_Success_ReturnsCreatedWithLocation()
    {
        var action = Result.Success("id-1").ToCreatedResult(id => $"/api/v1/items/{id}").ShouldBeOfType<CreatedResult>();

        action.Location.ShouldBe("/api/v1/items/id-1");
        action.Value.ShouldBeOfType<CommandResult<string>>().Data.ShouldBe("id-1");
    }

    [Fact]
    public void ToCreatedResult_Failure_DoesNotCreate() =>
        Result.Failure<string>(_duplicate).ToCreatedResult(id => id).ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(StatusCodes.Status409Conflict);
}
