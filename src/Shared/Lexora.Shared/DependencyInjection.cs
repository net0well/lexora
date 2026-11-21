using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddShared(this IServiceCollection services) => services;
}
