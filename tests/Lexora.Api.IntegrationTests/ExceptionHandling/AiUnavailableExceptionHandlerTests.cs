using Lexora.Api.ExceptionHandling;
using Lexora.Application.Abstractions.Ai;
using Microsoft.AspNetCore.Http;

namespace Lexora.Api.IntegrationTests.ExceptionHandling;

public sealed class AiUnavailableExceptionHandlerTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TryHandleAsync_ProviderUnavailable_RespondsServiceUnavailableWithRetryAfter()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var exception = new AiProviderUnavailableException("ollama", "local", TimeSpan.FromSeconds(1.2), new HttpRequestException("down"));

        var handled = await new AiUnavailableExceptionHandler().TryHandleAsync(context, exception, CancellationToken);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        context.Response.Headers.RetryAfter.ToString().ShouldBe("2");
        context.Response.Body.Position = 0;
        (await new StreamReader(context.Response.Body).ReadToEndAsync(CancellationToken)).ShouldContain("\"key\":\"ai.unavailable\"");
    }

    [Fact]
    public async Task TryHandleAsync_OtherFailure_IsLeftToTheNextHandler() =>
        (await new AiUnavailableExceptionHandler().TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException(), CancellationToken))
            .ShouldBeFalse();
}
