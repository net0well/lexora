using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lexora.Architecture.Tests.Sources;

internal sealed record SourceFile(string RelativePath, IReadOnlyList<string> TopLevelTypeNames)
{
    private static readonly CSharpParseOptions _parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

    public string ExpectedTypeName => Path.GetFileNameWithoutExtension(RelativePath);

    public static SourceFile Parse(string path)
    {
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), _parseOptions).GetCompilationUnitRoot();

        string[] typeNames = [.. root.DescendantNodes().Where(IsTopLevelType).Select(TypeNameOf)];

        return new SourceFile(Path.GetRelativePath(RepositoryRoot.FullPath, path), typeNames);
    }

    private static bool IsTopLevelType(SyntaxNode node) =>
        node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax
        && !node.Ancestors().Any(ancestor => ancestor is BaseTypeDeclarationSyntax);

    private static string TypeNameOf(SyntaxNode node) => node switch
    {
        TypeDeclarationSyntax type => WithTypeParameters(type.Identifier.Text, type.TypeParameterList),
        DelegateDeclarationSyntax @delegate => WithTypeParameters(@delegate.Identifier.Text, @delegate.TypeParameterList),
        BaseTypeDeclarationSyntax type => type.Identifier.Text,
        _ => throw new ArgumentOutOfRangeException(nameof(node), node.Kind(), "Not a type declaration."),
    };

    private static string WithTypeParameters(string name, TypeParameterListSyntax? typeParameters) =>
        typeParameters is null
            ? name
            : $"{name}Of{string.Concat(typeParameters.Parameters.Select(parameter => parameter.Identifier.Text))}";
}
