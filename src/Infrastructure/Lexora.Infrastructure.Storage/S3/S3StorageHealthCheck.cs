using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.Storage.S3;

public sealed class S3StorageHealthCheck(S3ClientFactory clients, IOptions<S3StorageOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await clients.Internal.ListObjectsV2Async(
                new ListObjectsV2Request { BucketName = options.Value.Bucket, MaxKeys = 1 },
                cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "The storage bucket is not reachable.", exception);
        }
    }
}
