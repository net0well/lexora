using Lexora.Application.Common.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Lexora.Api.Uploads;

public sealed class UploadSizeLimitFilter(IOptions<DocumentOptions> options) : IResourceFilter
{
    public const long EnvelopeAllowanceBytes = 1_048_576;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var limit = options.Value.MaxFileSizeBytes + EnvelopeAllowanceBytes;
        var features = context.HttpContext.Features;

        if (features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } bodySize)
        {
            bodySize.MaxRequestBodySize = limit;
        }

        if (features.Get<IFormFeature>() is null or { Form: null })
        {
            features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request, new FormOptions { MultipartBodyLengthLimit = limit }));
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
