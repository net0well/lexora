using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAiAdapter(this IServiceCollection services, IConfiguration configuration) => services;
}
