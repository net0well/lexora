using Lexora.Architecture.Tests.Projects;

namespace Lexora.Architecture.Tests;

public sealed class ProjectReferenceTests
{
    private const string Shared = "Lexora.Shared";
    private const string Domain = "Lexora.Domain";
    private const string Application = "Lexora.Application";
    private const string Hosting = "Lexora.Hosting";

    private static readonly string[] _adapters =
    [
        "Lexora.Infrastructure.AI",
        "Lexora.Infrastructure.Caching",
        "Lexora.Infrastructure.DocumentProcessing",
        "Lexora.Infrastructure.Identity",
        "Lexora.Infrastructure.Messaging",
        "Lexora.Infrastructure.Persistence",
        "Lexora.Infrastructure.Storage",
    ];

    private static readonly Dictionary<string, string[]> _layerTable = new()
    {
        [Shared] = [],
        [Domain] = [Shared],
        [Application] = [Domain, Shared],
        ["Lexora.Infrastructure.AI"] = [Application, Shared],
        ["Lexora.Infrastructure.Caching"] = [Application, Shared],
        ["Lexora.Infrastructure.DocumentProcessing"] = [Application, Domain, Shared],
        ["Lexora.Infrastructure.Identity"] = [Application, Domain, Shared],
        ["Lexora.Infrastructure.Messaging"] = [Application, Shared],
        ["Lexora.Infrastructure.Persistence"] = [Application, Domain, Shared],
        ["Lexora.Infrastructure.Storage"] = [Application, Shared],
        [Hosting] = [Shared],
        ["Lexora.Api"] = [Hosting, Application, .. _adapters],
        ["Lexora.Worker"] = [Hosting, Application, .. _adapters],
    };

    public static TheoryData<string> ProjectNames => [.. _layerTable.Keys];

    [Theory]
    [MemberData(nameof(ProjectNames))]
    public void SourceProject_ProjectReferences_MatchLayerTable(string projectName)
    {
        var project = RepositoryProjects.SourceNamed(projectName);

        project.ProjectReferences.Order().ShouldBe(_layerTable[projectName].Order());
    }

    [Fact]
    public void SourceProjects_WhenListed_AreAllCoveredByLayerTable()
    {
        var sourceProjectNames = RepositoryProjects.Source.Select(project => project.Name);

        sourceProjectNames.Order().ShouldBe(_layerTable.Keys.Order());
    }
}
