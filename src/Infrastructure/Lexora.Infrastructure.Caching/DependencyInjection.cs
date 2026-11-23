using Lexora.Application.Abstractions.Caching;
using Lexora.Shared.Configuration;
using Lexora.Shared.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.Caching;

public static class DependencyInjection
{
    public static IServiceCollection AddCachingAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CachingOptions>()
            .Bind(configuration.GetSection(CachingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHybridCache();
        services.AddSingleton<ICacheStore, HybridCacheStore>();

        // Without Redis the cache stays in memory, which is enough for tests and a single local instance.
        var redis = configuration.GetConnectionString(ConnectionStringNames.Redis);

        if (!string.IsNullOrWhiteSpace(redis))
        {
            var instanceName = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>()?.InstanceName ?? new CachingOptions().InstanceName;

            services.AddStackExchangeRedisCache(redisOptions =>
            {
                redisOptions.Configuration = redis;
                redisOptions.InstanceName = instanceName;
            });
            services.AddHealthChecks().AddCheck<RedisCacheHealthCheck>("redis", tags: [HealthCheckTags.Ready]);
        }

        return services;
    }
}
