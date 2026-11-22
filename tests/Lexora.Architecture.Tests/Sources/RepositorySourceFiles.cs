namespace Lexora.Architecture.Tests.Sources;

internal static class RepositorySourceFiles
{
    private static readonly string[] _scannedFolders = ["src", "tests"];

    private static readonly string[] _buildOutputFolders = ["bin", "obj"];

    private static readonly string _generatedMigrationsFolder =
        Path.Combine("src", "Infrastructure", "Lexora.Infrastructure.Persistence", "Migrations") + Path.DirectorySeparatorChar;

    private static readonly EnumerationOptions _enumeration = new()
    {
        RecurseSubdirectories = true,
        MatchType = MatchType.Simple,
    };

    public static IReadOnlyList<SourceFile> All { get; } =
    [
        .. _scannedFolders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(RepositoryRoot.FullPath, folder), "*.cs", _enumeration))
            .Select(path => Path.GetRelativePath(RepositoryRoot.FullPath, path))
            .Where(relativePath => !IsBuildOutput(relativePath) && !IsGeneratedMigration(relativePath))
            .Select(relativePath => SourceFile.Parse(Path.Combine(RepositoryRoot.FullPath, relativePath)))
    ];

    private static bool IsBuildOutput(string relativePath) =>
        relativePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => _buildOutputFolders.Contains(segment, StringComparer.OrdinalIgnoreCase));

    private static bool IsGeneratedMigration(string relativePath) =>
        relativePath.StartsWith(_generatedMigrationsFolder, StringComparison.OrdinalIgnoreCase);
}
