using System.Xml.Linq;

namespace Lexora.Architecture.Tests.Projects;

internal static class RepositoryProjects
{
    public static IReadOnlyList<ProjectFile> Source { get; } = LoadFrom("src");

    public static IReadOnlyList<ProjectFile> All { get; } = [.. Source, .. LoadFrom("tests")];

    public static IReadOnlyList<string> CentralPackageVersions { get; } = LoadCentralPackageVersions();

    public static ProjectFile SourceNamed(string name) => Source.Single(project => project.Name == name);

    private static ProjectFile[] LoadFrom(string folder) =>
    [
        .. Directory.EnumerateFiles(Path.Combine(RepositoryRoot.FullPath, folder), "*.csproj", SearchOption.AllDirectories)
            .Select(ProjectFile.Load)
    ];

    private static string[] LoadCentralPackageVersions() =>
    [
        .. XDocument.Load(Path.Combine(RepositoryRoot.FullPath, "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .Select(item => (string?)item.Attribute("Include"))
            .OfType<string>()
    ];
}
