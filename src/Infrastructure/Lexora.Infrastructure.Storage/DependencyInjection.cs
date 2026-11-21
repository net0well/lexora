using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.Storage;

public static class DependencyInjection
{
    public static IServiceCollection AddStorageAdapter(this IServiceCollection services, IConfiguration configuration) => services;
}
