using Microsoft.Extensions.Configuration;
using Wolverine;

namespace Lexora.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static WolverineOptions AddPersistenceAdapter(this WolverineOptions options, IConfiguration configuration) => options;
}
