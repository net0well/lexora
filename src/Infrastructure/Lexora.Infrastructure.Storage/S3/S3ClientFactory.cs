using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.Storage.S3;

public sealed class S3ClientFactory : IDisposable
{
    public S3ClientFactory(IOptions<S3StorageOptions> options)
    {
        var settings = options.Value;
        var credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);

        Internal = new AmazonS3Client(credentials, ConfigFor(settings.ServiceUrl!, settings));
        Public = new AmazonS3Client(credentials, ConfigFor(settings.PublicUrl ?? settings.ServiceUrl!, settings));
    }

    public IAmazonS3 Internal { get; }

    public IAmazonS3 Public { get; }

    public void Dispose()
    {
        Internal.Dispose();
        Public.Dispose();
    }

    // S3-compatible services such as SeaweedFS do not all accept the trailing checksums
    // the SDK sends by default, so checksums are only used where the operation requires them.
    private static AmazonS3Config ConfigFor(Uri serviceUrl, S3StorageOptions settings) => new()
    {
        ServiceURL = serviceUrl.ToString(),
        AuthenticationRegion = settings.Region,
        RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region),
        ForcePathStyle = settings.ForcePathStyle,
        UseHttp = serviceUrl.Scheme == Uri.UriSchemeHttp,
        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
    };
}
