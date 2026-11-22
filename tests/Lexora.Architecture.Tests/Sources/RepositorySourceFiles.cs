namespace Lexora.Architecture.Tests.Sources;

internal static class RepositorySourceFiles
{
    private static readonly string[] _scannedFolders = ["src", "tests"];

    private static readonly string[] _buildOutputFolders = ["bin", "obj"];

    private static readonly EnumerationOptions _enumeration = new()
    {
        RecurseSubdirectories = true,
        MatchType = MatchType.Simple,
    };

    public static IReadOnlyList<SourceFile> All { get; } =
    [
        .. _scannedFolders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(RepositoryRoot.FullPath, folder), "*.cs", _enumeration))
            .Where(path => !IsBuildOutput(path))
            .Select(SourceFile.Parse)
    ];

    private static bool IsBuildOutput(string path) =>
        Path.GetRelativePath(RepositoryRoot.FullPath, path)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => _buildOutputFolders.Contains(segment, StringComparer.OrdinalIgnoreCase));
}
