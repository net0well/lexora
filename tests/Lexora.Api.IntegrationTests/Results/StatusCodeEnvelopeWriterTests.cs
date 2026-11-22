using System.Text.Json;
using Lexora.Api.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Lexora.Api.IntegrationTests.Results;

public sealed class StatusCodeEnvelopeWriterTests
{
    [Theory]
    [InlineData(StatusCodes.Status404NotFound, "request.notFound")]
    [InlineData(StatusCodes.Status405MethodNotAllowed, "request.methodNotAllowed")]
    [InlineData(StatusCodes.Status415UnsupportedMediaType, "request.unsupportedMediaType")]
    [InlineData(StatusCodes.Status400BadRequest, "request.failed")]
    public async Task WriteAsync_EmptyErrorResponse_WritesTheEnvelopeKeepingTheStatus(int statusCode, string expectedKey)
    {
        var httpContext = new DefaultHttpContext { Response = { StatusCode = statusCode, Body = new MemoryStream() } };

        await StatusCodeEnvelopeWriter.WriteAsync(new StatusCodeContext(httpContext, new StatusCodePagesOptions(), _ => Task.CompletedTask));

        httpContext.Response.StatusCode.ShouldBe(statusCode);
        httpContext.Response.Body.Position = 0;
        var body = (await JsonDocument.ParseAsync(httpContext.Response.Body, cancellationToken: TestContext.Current.CancellationToken)).RootElement;
        body.GetProperty("success").GetBoolean().ShouldBeFalse();
        body.GetProperty("notifications")[0].GetProperty("key").GetString().ShouldBe(expectedKey);
    }
}
