using Amazon.S3.Model;
using DotNet.Testcontainers.Containers;
using Lexora.Infrastructure.Storage;
using Lexora.Infrastructure.Storage.S3;
using Lexora.TestKit.Containers;

namespace Lexora.Infrastructure.IntegrationTests.Storage;

public sealed class SeaweedFsFixture : IAsyncLifetime
{
    public const string Bucket = "lexora-tests";

    private readonly IContainer _container = SeaweedFsContainerFactory.Create();

    public S3StorageOptions Options { get; private set; } = new();

    public S3ClientFactory Clients { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync(TestContext.Current.CancellationToken);

        Options = For(Bucket);
        Clients = new S3ClientFactory(Microsoft.Extensions.Options.Options.Create(Options));
        await Clients.Internal.PutBucketAsync(new PutBucketRequest { BucketName = Bucket }, TestContext.Current.CancellationToken);
    }

    public S3StorageOptions For(string bucket) => new()
    {
        ServiceUrl = SeaweedFsContainerFactory.ServiceUrlOf(_container),
        Bucket = bucket,
        AccessKey = SeaweedFsContainerFactory.AccessKey,
        SecretKey = SeaweedFsContainerFactory.SecretKey,
    };

    public S3FileStorage CreateStorage(TimeProvider timeProvider) =>
        new(Clients, Microsoft.Extensions.Options.Options.Create(Options), Microsoft.Extensions.Options.Options.Create(new StorageOptions()), timeProvider);

    public async ValueTask DisposeAsync()
    {
        Clients?.Dispose();
        await _container.DisposeAsync();
    }
}
