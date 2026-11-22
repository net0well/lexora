using System.Reflection;
using Lexora.Architecture.Tests.Projects;
using NetArchTest.Rules;

namespace Lexora.Architecture.Tests;

public sealed class LayerDependencyTests
{
    private static readonly string[] _domainAllowedDependencies =
    [
        "System",
        "Lexora.Domain",
        "Lexora.Shared",
        "Microsoft.Extensions.DependencyInjection",
    ];

    private static readonly string[] _outerLayers =
    [
        "Lexora.Infrastructure",
        "Lexora.Hosting",
        "Lexora.Api",
        "Lexora.Worker",
        "Lexora.Mcp",
    ];

    private static readonly string[] _technicalFrameworks =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Amazon",
        "OllamaSharp",
        "Anthropic",
        "OpenAI",
        "ModelContextProtocol",
        "Microsoft.AspNetCore",
        "System.IdentityModel.Tokens.Jwt",
        "Microsoft.IdentityModel",
    ];

    private static readonly string[] _hostNamespaces = ["Lexora.Api", "Lexora.Worker", "Lexora.Mcp"];

    [Fact]
    public void SolutionAssemblies_WhenListed_CoverEverySourceProject()
    {
        var assemblyNames = SolutionAssemblies.All.Select(SolutionAssemblies.NameOf);

        assemblyNames.Order().ShouldBe(RepositoryProjects.Source.Select(project => project.Name).Order());
    }

    [Fact]
    public void Domain_Dependencies_AreLimitedToSharedAndBaseLibrary() =>
        LexoraTypesIn(SolutionAssemblies.Domain)
            .Should().OnlyHaveDependenciesOn(_domainAllowedDependencies)
            .GetResult()
            .ShouldBeSuccessful();

    [Fact]
    public void Application_Dependencies_ExcludeAdaptersAndHosts() =>
        LexoraTypesIn(SolutionAssemblies.Application)
            .ShouldNot().HaveDependencyOnAny(_outerLayers)
            .GetResult()
            .ShouldBeSuccessful();

    [Fact]
    public void Application_Dependencies_ExcludeTechnicalFrameworks() =>
        LexoraTypesIn(SolutionAssemblies.Application)
            .ShouldNot().HaveDependencyOnAny(_technicalFrameworks)
            .GetResult()
            .ShouldBeSuccessful();

    [Theory]
    [MemberData(nameof(SolutionAssemblies.AdapterNames), MemberType = typeof(SolutionAssemblies))]
    public void Adapter_Dependencies_ExcludeOtherAdapters(string adapterName)
    {
        string[] otherAdapters =
        [
            .. SolutionAssemblies.Adapters
                .Select(SolutionAssemblies.NameOf)
                .Where(name => name != adapterName)
        ];

        LexoraTypesIn(SolutionAssemblies.Named(adapterName))
            .ShouldNot().HaveDependencyOnAny(otherAdapters)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Theory]
    [MemberData(nameof(SolutionAssemblies.NonHostNames), MemberType = typeof(SolutionAssemblies))]
    public void NonHostAssembly_Dependencies_ExcludeHosts(string assemblyName) =>
        LexoraTypesIn(SolutionAssemblies.Named(assemblyName))
            .ShouldNot().HaveDependencyOnAny(_hostNamespaces)
            .GetResult()
            .ShouldBeSuccessful();

    [Theory]
    [MemberData(nameof(SolutionAssemblies.CoreAndAdapterNames), MemberType = typeof(SolutionAssemblies))]
    public void CoreOrAdapterAssembly_Dependencies_ExcludeHostingDefaults(string assemblyName) =>
        LexoraTypesIn(SolutionAssemblies.Named(assemblyName))
            .ShouldNot().HaveDependencyOn("Lexora.Hosting")
            .GetResult()
            .ShouldBeSuccessful();

    private static PredicateList LexoraTypesIn(Assembly assembly) =>
        Types.InAssembly(assembly).That().ResideInNamespaceStartingWith("Lexora");
}
