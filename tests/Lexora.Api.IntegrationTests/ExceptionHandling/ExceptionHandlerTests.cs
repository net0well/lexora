using System.Text.Json;
using Lexora.Api.ExceptionHandling;
using Lexora.Application.Common.Validation;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexora.Api.IntegrationTests.ExceptionHandling;

public sealed class ExceptionHandlerTests
{
    [Fact]
    public async Task InputValidationHandler_ValidationException_RespondsBadRequestWithNotifications()
    {
        var context = NewContext();
        var exception = new InputValidationException([Notification.Validation("knowledgeBase.name.required", "The name is required.")]);

        var handled = await new InputValidationExceptionHandler().TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var body = await ReadBodyAsync(context);
        body.GetProperty("success").GetBoolean().ShouldBeFalse();
        body.GetProperty("notifications")[0].GetProperty("key").GetString().ShouldBe("knowledgeBase.name.required");
    }

    [Fact]
    public async Task InputValidationHandler_OtherException_LeavesItToTheNextHandler() =>
        (await new InputValidationExceptionHandler().TryHandleAsync(NewContext(), new InvalidOperationException(), TestContext.Current.CancellationToken))
            .ShouldBeFalse();

    [Fact]
    public async Task UnhandledHandler_AnyException_RespondsWithUnexpectedKeyAndHidesDetails()
    {
        var context = NewContext();

        var handled = await new UnhandledExceptionHandler(NullLogger<UnhandledExceptionHandler>.Instance)
            .TryHandleAsync(context, new InvalidOperationException("connection string with secret"), TestContext.Current.CancellationToken);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        var notification = (await ReadBodyAsync(context)).GetProperty("notifications")[0];
        notification.GetProperty("key").GetString().ShouldBe(UnhandledExceptionHandler.Key);
        notification.GetProperty("message").GetString().ShouldNotBeNull().ShouldNotContain("secret");
    }

    private static DefaultHttpContext NewContext() => new() { Response = { Body = new MemoryStream() } };

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;

        return (await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken)).RootElement;
    }
}
