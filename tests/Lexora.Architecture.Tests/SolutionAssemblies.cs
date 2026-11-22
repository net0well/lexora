using System.Reflection;
using Lexora.Application;

namespace Lexora.Architecture.Tests;

internal static class SolutionAssemblies
{
    public static Assembly Shared { get; } = typeof(Lexora.Shared.DependencyInjection).Assembly;

    public static Assembly Domain { get; } = typeof(Lexora.Domain.DependencyInjection).Assembly;

    public static Assembly Application { get; } = typeof(ApplicationAssembly).Assembly;

    public static IReadOnlyList<Assembly> Adapters { get; } =
    [
        typeof(Lexora.Infrastructure.AI.DependencyInjection).Assembly,
        typeof(Lexora.Infrastructure.Caching.DependencyInjection).Assembly,
        typeof(Lexora.Infrastructure.DocumentProcessing.DependencyInjection).Assembly,
        typeof(Lexora.Infrastructure.Identity.DependencyInjection).Assembly,
        typeof(Lexora.Infrastructure.Messaging.DependencyInjection).Assembly,
        typeof(Lexora.Infrastructure.Persistence.DependencyInjection).Assembly,
        typeof(Lexora.Infrastructure.Storage.DependencyInjection).Assembly,
    ];

    public static Assembly Hosting { get; } = typeof(Lexora.Hosting.DependencyInjection).Assembly;

    public static IReadOnlyList<Assembly> Hosts { get; } =
    [
        typeof(Lexora.Api.DependencyInjection).Assembly,
        typeof(Lexora.Worker.DependencyInjection).Assembly,
    ];

    public static IReadOnlyList<Assembly> CoreAndAdapters { get; } = [Shared, Domain, Application, .. Adapters];

    public static IReadOnlyList<Assembly> NonHosts { get; } = [.. CoreAndAdapters, Hosting];

    public static IReadOnlyList<Assembly> All { get; } = [.. NonHosts, .. Hosts];

    public static TheoryData<string> AdapterNames => [.. Adapters.Select(NameOf)];

    public static TheoryData<string> CoreAndAdapterNames => [.. CoreAndAdapters.Select(NameOf)];

    public static TheoryData<string> NonHostNames => [.. NonHosts.Select(NameOf)];

    public static Assembly Named(string name) => All.Single(assembly => NameOf(assembly) == name);

    public static string NameOf(Assembly assembly) => assembly.GetName().Name ?? string.Empty;
}
