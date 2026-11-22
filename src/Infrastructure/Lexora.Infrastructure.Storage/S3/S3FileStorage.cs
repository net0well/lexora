using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Lexora.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.Storage.S3;

public sealed class S3FileStorage(
    S3ClientFactory clients,
    IOptions<S3StorageOptions> s3Options,
    IOptions<StorageOptions> storageOptions,
    TimeProvider timeProvider) : IFileStorage
{
    private const int DeleteBatchSize = 1000;

    private readonly string _bucket = s3Options.Value.Bucket;

    private readonly Protocol _publicProtocol =
        (s3Options.Value.PublicUrl ?? s3Options.Value.ServiceUrl)?.Scheme == Uri.UriSchemeHttp ? Protocol.HTTP : Protocol.HTTPS;

    public async Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var start = content.Position;

        await clients.Internal.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
            },
            cancellationToken);

        return new StoredFile(key, content.Length - start);
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await clients.Internal.GetObjectAsync(_bucket, key, cancellationToken);

            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"No stored file under '{key}'.", key, exception);
        }
    }

    public async Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix };

        do
        {
            var page = await clients.Internal.ListObjectsV2Async(request, cancellationToken);
            var keys = page.S3Objects ?? [];

            foreach (var batch in keys.Chunk(DeleteBatchSize))
            {
                await clients.Internal.DeleteObjectsAsync(
                    new DeleteObjectsRequest
                    {
                        BucketName = _bucket,
                        Objects = [.. batch.Select(item => new KeyVersion { Key = item.Key })],
                    },
                    cancellationToken);
            }

            request.ContinuationToken = page.IsTruncated == true ? page.NextContinuationToken : null;
        }
        while (request.ContinuationToken is not null);
    }

    public async Task<FileDownload> CreateDownloadAsync(string key, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var expiresAt = timeProvider.GetUtcNow() + storageOptions.Value.DownloadUrlLifetime;

        var url = await clients.Public.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Protocol = _publicProtocol,
            Expires = expiresAt.UtcDateTime,
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = AttachmentDisposition.For(fileName),
                ContentType = contentType,
            },
        });

        return new FileDownload(new Uri(url), expiresAt);
    }
}
