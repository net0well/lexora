namespace Lexora.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddWorker(this IServiceCollection services, IConfiguration configuration) => services;
}
