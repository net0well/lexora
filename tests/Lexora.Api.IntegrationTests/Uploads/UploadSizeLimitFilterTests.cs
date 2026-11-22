using Lexora.Api.Uploads;
using Lexora.Application.Common.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Lexora.Api.IntegrationTests.Uploads;

public sealed class UploadSizeLimitFilterTests
{
    [Fact]
    public void OnResourceExecuting_UploadRequest_RaisesBothLimitsToTheFileLimitPlusTheEnvelope()
    {
        var bodySize = new WritableBodySizeFeature();
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(bodySize);
        var context = new ResourceExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            [],
            []);

        new UploadSizeLimitFilter(Options.Create(new DocumentOptions { MaxFileSizeBytes = 5_000 })).OnResourceExecuting(context);

        var expected = 5_000 + UploadSizeLimitFilter.EnvelopeAllowanceBytes;
        bodySize.MaxRequestBodySize.ShouldBe(expected);
        httpContext.Features.Get<IFormFeature>().ShouldNotBeNull();
    }

    private sealed class WritableBodySizeFeature : IHttpMaxRequestBodySizeFeature
    {
        public bool IsReadOnly => false;

        public long? MaxRequestBodySize { get; set; } = 1_048_576;
    }
}
