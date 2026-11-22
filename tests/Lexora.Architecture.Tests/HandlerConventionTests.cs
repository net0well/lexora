using System.Reflection;
using Lexora.Architecture.Tests.Sources;
using Lexora.Domain.Abstractions;
using Wolverine.Attributes;

namespace Lexora.Architecture.Tests;

public sealed class HandlerConventionTests
{
    private const string CommandLineFolder = "src/Hosts/Lexora.Api/CommandLine/";

    private static readonly HashSet<string> _saveChanges = ["SaveChanges", "SaveChangesAsync"];

    [Fact]
    public void HandlerScan_ApplicationAssembly_FindsCommandAndQueryHandlers()
    {
        var messages = HandleMethods().Select(method => method.GetParameters()[0].ParameterType.Name).ToList();

        messages.ShouldContain("LoginCommand");
        messages.ShouldContain("GetCurrentUserQuery");
    }

    [Fact]
    public void CommandHandlers_HandleMethods_AreTransactionalOverTheUnitOfWork()
    {
        var violations = HandleMethods()
            .Where(method => MessageName(method).EndsWith("Command", StringComparison.Ordinal))
            .Where(method => method.GetCustomAttribute<TransactionalAttribute>()?.DbContextType != typeof(IUnitOfWork))
            .Select(Describe);

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void QueryHandlers_HandleMethods_AreNotTransactional()
    {
        var violations = HandleMethods()
            .Where(method => MessageName(method).EndsWith("Query", StringComparison.Ordinal))
            .Where(method => method.GetCustomAttribute<TransactionalAttribute>() is not null)
            .Select(Describe);

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void ProductionCode_SaveChanges_IsOnlyCalledFromTheCommandLine()
    {
        var violations = RepositorySourceFiles.All
            .Where(file => file.IsProductionCode && !file.RelativePath.Replace('\\', '/').StartsWith(CommandLineFolder, StringComparison.Ordinal))
            .SelectMany(file => file.InvokedMethodNames.Where(_saveChanges.Contains).Select(method => $"{file.RelativePath}: {method}"));

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void SourceScan_CommandLine_SeesTheSaveItIsAllowedToMake()
    {
        var seedAdmin = RepositorySourceFiles.All.Single(file => file.RelativePath.EndsWith("SeedAdminCliCommand.cs", StringComparison.Ordinal));

        seedAdmin.InvokedMethodNames.ShouldContain("SaveChangesAsync");
    }

    private static IEnumerable<MethodInfo> HandleMethods() =>
        SolutionAssemblies.Application.GetTypes()
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.Name is "Handle" or "HandleAsync" && method.GetParameters().Length > 0);

    private static string MessageName(MethodInfo method) => method.GetParameters()[0].ParameterType.Name;

    private static string Describe(MethodInfo method) => $"{method.DeclaringType!.FullName}.{method.Name}({MessageName(method)})";
}
