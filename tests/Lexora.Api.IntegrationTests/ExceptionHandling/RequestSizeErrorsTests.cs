using Lexora.Api.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lexora.Api.IntegrationTests.ExceptionHandling;

public sealed class RequestSizeErrorsTests
{
    [Fact]
    public void IsTooLarge_KestrelBodyLimit_IsRecognizedEvenWhenWrapped() =>
        RequestSizeErrors.IsTooLarge(new IOException("read failed", new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge)))
            .ShouldBeTrue();

    [Fact]
    public void IsTooLarge_MultipartLengthLimit_IsRecognized() =>
        RequestSizeErrors.IsTooLarge(new InvalidDataException("Multipart body length limit 1024 exceeded.")).ShouldBeTrue();

    [Fact]
    public void IsTooLarge_ModelErrorMessageFromTheFormReader_IsRecognized() =>
        RequestSizeErrors.IsTooLarge(new ModelError("Failed to read the request form. Multipart body length limit 1024 exceeded.")).ShouldBeTrue();

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status408RequestTimeout)]
    public void IsTooLarge_OtherBadRequests_AreNotSizeErrors(int statusCode) =>
        RequestSizeErrors.IsTooLarge(new BadHttpRequestException("bad", statusCode)).ShouldBeFalse();

    [Fact]
    public void IsTooLarge_MalformedJson_IsNotASizeError() =>
        RequestSizeErrors.IsTooLarge(new ModelError("'S' is an invalid start of a value.")).ShouldBeFalse();
}
