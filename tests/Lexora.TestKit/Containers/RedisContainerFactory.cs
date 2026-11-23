using Testcontainers.Redis;

namespace Lexora.TestKit.Containers;

public static class RedisContainerFactory
{
    public const string Image = "redis:8-alpine";

    public static RedisContainer Create() => new RedisBuilder(Image).Build();
}
