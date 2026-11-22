using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Lexora.TestKit.Containers;

public static class SeaweedFsContainerFactory
{
    public const string Image = "chrislusf/seaweedfs:latest";
    public const string AccessKey = "lexora-tests";
    public const string SecretKey = "lexora-tests-secret";
    public const int S3Port = 8333;

    private const string ConfigPath = "/etc/seaweedfs/s3.json";

    private static readonly string _config = $$"""
        {
          "identities": [
            {
              "name": "tests",
              "credentials": [{ "accessKey": "{{AccessKey}}", "secretKey": "{{SecretKey}}" }],
              "actions": ["Admin", "Read", "Write", "List", "Tagging"]
            }
          ]
        }
        """;

    public static IContainer Create() =>
        new ContainerBuilder(Image)
            .WithCommand("server", "-dir=/data", "-s3", $"-s3.port={S3Port}", $"-s3.config={ConfigPath}", "-ip.bind=0.0.0.0")
            .WithResourceMapping(Encoding.UTF8.GetBytes(_config), FilePath.Of(ConfigPath))
            .WithPortBinding(S3Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
                .ForPort(S3Port)
                .ForPath("/")
                .ForStatusCodeMatching(status => (int)status < 500)))
            .Build();

    public static Uri ServiceUrlOf(IContainer container) => new($"http://{container.Hostname}:{container.GetMappedPublicPort(S3Port)}");
}
