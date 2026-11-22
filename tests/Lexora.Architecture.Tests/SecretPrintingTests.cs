using System.Reflection;

namespace Lexora.Architecture.Tests;

public sealed class SecretPrintingTests
{
    private const string Sentinel = "sentinel-";

    [Fact]
    public void Records_HoldingPasswordsOrTokens_KeepThemOutOfToString()
    {
        var records = SolutionAssemblies.All
            .SelectMany(assembly => assembly.GetTypes())
            .Where(IsRecord)
            .Where(type => SecretParameters(type).Any())
            .ToList();

        records.ShouldNotBeEmpty();

        var leaks = records
            .SelectMany(type => LeakedParameters(type).Select(parameter => $"{type.FullName}.{parameter}"))
            .ToList();

        leaks.ShouldBeEmpty();
    }

    private static IEnumerable<string> LeakedParameters(Type type)
    {
        var constructor = PrimaryConstructor(type);
        var arguments = constructor.GetParameters().Select(SampleValue).ToArray();
        var printed = constructor.Invoke(arguments).ToString() ?? string.Empty;

        return SecretParameters(type).Where(parameter => printed.Contains(Sentinel + parameter, StringComparison.Ordinal));
    }

    private static IEnumerable<string> SecretParameters(Type type) =>
        PrimaryConstructor(type).GetParameters()
            .Where(parameter => parameter.ParameterType == typeof(string) && IsSecret(type, parameter.Name!))
            .Select(parameter => parameter.Name!);

    private static bool IsSecret(Type type, string name) =>
        name.Contains("Password", StringComparison.Ordinal)
        || name.Contains("Secret", StringComparison.Ordinal)
        || name.EndsWith("Token", StringComparison.Ordinal)
        || (type.Name.EndsWith("Token", StringComparison.Ordinal) && name is "Value" or "Hash");

    private static object? SampleValue(ParameterInfo parameter) =>
        parameter.ParameterType == typeof(string) ? Sentinel + parameter.Name
        : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType)
        : null;

    private static ConstructorInfo PrimaryConstructor(Type type) =>
        type.GetConstructors().OrderByDescending(constructor => constructor.GetParameters().Length).First();

    private static bool IsRecord(Type type) =>
        type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
        && type.GetMethod("<Clone>$") is not null
        && type.GetConstructors().Length > 0;
}
