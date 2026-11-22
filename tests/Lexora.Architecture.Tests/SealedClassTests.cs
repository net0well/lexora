using System.Runtime.CompilerServices;

namespace Lexora.Architecture.Tests;

public sealed class SealedClassTests
{
    private const string DbContextTypeName = "Microsoft.EntityFrameworkCore.DbContext";

    [Fact]
    public void ConcreteClass_InSourceAssemblies_IsSealed()
    {
        Type[] types = [.. SolutionAssemblies.All.SelectMany(assembly => assembly.GetTypes())];
        var inheritedTypes = types.Select(BaseDefinitionOf).OfType<Type>().ToHashSet();

        var unsealed = types
            .Where(type => IsLexoraConcreteClass(type) && !type.IsSealed)
            .Where(type => !inheritedTypes.Contains(type) && !InheritsDbContext(type))
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

    private static bool InheritsDbContext(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.FullName == DbContextTypeName)
            {
                return true;
            }
        }

        return false;
    }
}
