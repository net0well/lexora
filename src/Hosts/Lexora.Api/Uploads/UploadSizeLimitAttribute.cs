using Microsoft.AspNetCore.Mvc;

namespace Lexora.Api.Uploads;

[AttributeUsage(AttributeTargets.Method)]
public sealed class UploadSizeLimitAttribute : TypeFilterAttribute
{
    public UploadSizeLimitAttribute()
        : base(typeof(UploadSizeLimitFilter))
    {
    }
}
