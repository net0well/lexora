using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityAdapter(this IServiceCollection services, IConfiguration configuration) => services;
}
