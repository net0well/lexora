using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.Caching;

public static class DependencyInjection
{
    public static IServiceCollection AddCachingAdapter(this IServiceCollection services, IConfiguration configuration) => services;
}
