using System.Reflection;
using System.Runtime.CompilerServices;
using Lexora.Shared.Domain;

namespace Lexora.Architecture.Tests;

public sealed class DomainImmutabilityTests
{
    private const BindingFlags DeclaredPublicProperties =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Type[] _domainTypes =
    [
        .. SolutionAssemblies.Domain.GetTypes().Where(type =>
            type.Namespace?.StartsWith("Lexora.Domain", StringComparison.Ordinal) == true
            && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)),
    ];

    [Fact]
    public void DomainType_Properties_HaveNoPublicSetters()
    {
        var violations = _domainTypes
            .Where(type => !IsDomainEvent(type))
            .SelectMany(PublicSettersOf)
            .Select(property => $"{property.DeclaringType?.FullName}.{property.Name}");

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvent_PublicSetters_AreInitOnly()
    {
        var violations = _domainTypes
            .Where(IsDomainEvent)
            .SelectMany(PublicSettersOf)
            .Where(property => !IsInitOnly(property))
            .Select(property => $"{property.DeclaringType?.FullName}.{property.Name}");

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void DomainTypes_WhenScanned_IncludeAggregatesAndEvents()
    {
        _domainTypes.ShouldContain(type => type.Name == "Document");
        _domainTypes.ShouldContain(type => type.Name == "DocumentUploaded");
    }

    private static bool IsDomainEvent(Type type) => typeof(IDomainEvent).IsAssignableFrom(type);

    private static IEnumerable<PropertyInfo> PublicSettersOf(Type type) =>
        type.GetProperties(DeclaredPublicProperties).Where(property => property.SetMethod is { IsPublic: true });

    private static bool IsInitOnly(PropertyInfo property) =>
        property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)) == true;
}
