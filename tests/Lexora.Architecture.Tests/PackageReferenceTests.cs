using Lexora.Architecture.Tests.Projects;

namespace Lexora.Architecture.Tests;

public sealed class PackageReferenceTests
{
    private static readonly string[] _mappingLibraries = ["AutoMapper", "Mapster", "Riok.Mapperly"];

    private static readonly Dictionary<string, string[]> _allowedCorePackages = new()
    {
        ["Lexora.Shared"] = ["Microsoft.Extensions.*.Abstractions"],
        ["Lexora.Domain"] = [],
        ["Lexora.Application"] =
        [
            "WolverineFx",
            "FluentValidation",
            "Microsoft.Extensions.*.Abstractions",
            "Microsoft.Extensions.Options.DataAnnotations",
        ],
    };

    public static TheoryData<string> CoreProjectNames => [.. _allowedCorePackages.Keys];

    [Fact]
    public void Repository_PackageReferences_ExcludeMappingLibraries()
    {
        var packages = RepositoryProjects.All
            .SelectMany(project => project.PackageReferences)
            .Concat(RepositoryProjects.CentralPackageVersions);

        packages.Where(IsMappingLibrary).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(CoreProjectNames))]
    public void CoreProject_PackageReferences_AreLimitedToAllowedPackages(string projectName)
    {
        var allowed = _allowedCorePackages[projectName];

        var packages = RepositoryProjects.SourceNamed(projectName).PackageReferences;

        packages.Where(package => !allowed.Any(pattern => Matches(package, pattern))).ShouldBeEmpty();
    }

    private static bool IsMappingLibrary(string package) =>
        _mappingLibraries.Any(library =>
            package.Equals(library, StringComparison.OrdinalIgnoreCase)
            || package.StartsWith($"{library}.", StringComparison.OrdinalIgnoreCase));

    private static bool Matches(string package, string pattern)
    {
        var wildcard = pattern.IndexOf('*', StringComparison.Ordinal);

        if (wildcard < 0)
        {
            return package.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        var prefix = pattern[..wildcard];
        var suffix = pattern[(wildcard + 1)..];

        return package.Length > prefix.Length + suffix.Length
            && package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && package.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }
}
