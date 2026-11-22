using Lexora.Api.Contracts;
using Lexora.Api.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Lexora.Api.IntegrationTests.Results;

public sealed class InvalidModelStateResponseFactoryTests
{
    [Fact]
    public void Create_MalformedField_RespondsBadRequestNamingTheFieldOnly()
    {
        var context = NewContext();
        context.ModelState.AddModelError("$.email", "'S3cret' is an invalid start of a value.");

        var envelope = Envelope(InvalidModelStateResponseFactory.Create(context));

        var notification = envelope.Notifications.ShouldHaveSingleItem();
        notification.Key.ShouldBe(InvalidModelStateResponseFactory.Key);
        notification.Message.ShouldBe("The field '$.email' is malformed.");
    }

    [Fact]
    public void Create_MissingBody_ReportsTheBodyAsMalformed()
    {
        var context = NewContext();
        context.ModelState.AddModelError(string.Empty, "A non-empty request body is required.");

        Envelope(InvalidModelStateResponseFactory.Create(context)).Notifications.ShouldHaveSingleItem()
            .Message.ShouldBe("The request body is missing or malformed.");
    }

    private static ActionContext NewContext() => new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

    private static CommandResult Envelope(IActionResult action) =>
        action.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<CommandResult>();
}
