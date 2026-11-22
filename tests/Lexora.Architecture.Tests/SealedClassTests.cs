using System.Runtime.CompilerServices;

namespace Lexora.Architecture.Tests;

public sealed class SealedClassTests
{
    private static readonly HashSet<string> _frameworkBaseTypes =
    [
        "Microsoft.EntityFrameworkCore.DbContext",
        "Microsoft.EntityFrameworkCore.Migrations.Migration",
        "Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot",
    ];

    [Fact]
    public void ConcreteClass_InSourceAssemblies_IsSealed()
    {
        Type[] types = [.. SolutionAssemblies.All.SelectMany(assembly => assembly.GetTypes())];
        var inheritedTypes = types.Select(BaseDefinitionOf).OfType<Type>().ToHashSet();

        var unsealed = types
            .Where(type => IsLexoraConcreteClass(type) && !type.IsSealed)
            .Where(type => !inheritedTypes.Contains(type) && !InheritsFrameworkBaseType(type))
            .Select(type => type.FullName);

        unsealed.ShouldBeEmpty();
    }

    private static bool IsLexoraConcreteClass(Type type) =>
        type is { IsClass: true, IsAbstract: false }
        && type.Namespace?.StartsWith("Lexora", StringComparison.Ordinal) == true
        && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);

    private static Type? BaseDefinitionOf(Type type) =>
        type.BaseType is { IsGenericType: true } genericBase
            ? genericBase.GetGenericTypeDefinition()
            : type.BaseType;

    private static bool InheritsFrameworkBaseType(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.FullName is { } name && _frameworkBaseTypes.Contains(name))
            {
                return true;
            }
        }

        return false;
    }
}
