using System.Xml.Linq;

namespace Lexora.Architecture.Tests.Projects;

internal sealed record ProjectFile(string Name, IReadOnlyList<string> ProjectReferences, IReadOnlyList<string> PackageReferences)
{
    public static ProjectFile Load(string path)
    {
        var document = XDocument.Load(path);

        return new ProjectFile(
            Path.GetFileNameWithoutExtension(path),
            [.. IncludesOf(document, "ProjectReference").Select(ProjectNameOf)],
            [.. IncludesOf(document, "PackageReference")]);
    }

    private static IEnumerable<string> IncludesOf(XDocument document, string itemName) =>
        document.Descendants(itemName)
            .Select(item => (string?)item.Attribute("Include"))
            .OfType<string>();

    private static string ProjectNameOf(string include) =>
        Path.GetFileNameWithoutExtension(include.Replace('\\', Path.DirectorySeparatorChar));
}
