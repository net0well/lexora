using Lexora.Architecture.Tests.Sources;

namespace Lexora.Architecture.Tests;

public sealed class MigrationPolicyTests
{
    private static readonly HashSet<string> _automaticSchemaChanges = ["Migrate", "MigrateAsync", "EnsureCreated", "EnsureCreatedAsync"];

    [Fact]
    public void ProductionCode_Invocations_NeverApplyMigrationsOrCreateTheSchema()
    {
        var violations = RepositorySourceFiles.All
            .Where(file => file.IsProductionCode)
            .SelectMany(file => file.InvokedMethodNames
                .Where(_automaticSchemaChanges.Contains)
                .Select(method => $"{file.RelativePath}: {method}"));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void SourceScan_TestFixture_SeesTheMigrationItIsAllowedToRun()
    {
        var fixture = RepositorySourceFiles.All.Single(file => file.RelativePath.EndsWith("DatabaseFixture.cs", StringComparison.Ordinal));

        fixture.IsProductionCode.ShouldBeFalse();
        fixture.InvokedMethodNames.ShouldContain("MigrateAsync");
    }
}
