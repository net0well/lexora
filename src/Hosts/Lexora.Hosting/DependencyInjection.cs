using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lexora.Hosting;

public static class DependencyInjection
{
    public static IServiceCollection AddHostingDefaults(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment) => services;
}
