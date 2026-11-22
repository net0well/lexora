using Lexora.Architecture.Tests.Sources;

namespace Lexora.Architecture.Tests;

public sealed class SourceFileTests
{
    [Fact]
    public void RepositorySourceFiles_WhenScanned_IncludeSourceAndTestFiles()
    {
        var paths = RepositorySourceFiles.All.Select(file => file.RelativePath.Replace('\\', '/'));

        paths.ShouldContain("src/Shared/Lexora.Shared/DependencyInjection.cs");
        paths.ShouldContain("tests/Lexora.Architecture.Tests/SourceFileTests.cs");
    }

    [Fact]
    public void SourceFile_TopLevelTypes_AreAtMostOne()
    {
        var violations = RepositorySourceFiles.All
            .Where(file => file.TopLevelTypeNames.Count > 1)
            .Select(file => $"{file.RelativePath}: {string.Join(", ", file.TopLevelTypeNames)}");

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void SourceFile_WithSingleType_IsNamedAfterThatType()
    {
        var violations = RepositorySourceFiles.All
            .Where(file => file.TopLevelTypeNames.Count == 1 && file.TopLevelTypeNames[0] != file.ExpectedTypeName)
            .Select(file => $"{file.RelativePath}: {file.TopLevelTypeNames[0]}");

        violations.ShouldBeEmpty();
    }
}
