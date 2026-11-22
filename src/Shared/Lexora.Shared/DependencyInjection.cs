using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lexora.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddShared(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
