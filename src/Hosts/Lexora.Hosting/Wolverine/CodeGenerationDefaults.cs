using System.Reflection;
using JasperFx;
using JasperFx.CodeGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lexora.Hosting.Wolverine;

internal static class CodeGenerationDefaults
{
    public static void Apply(IServiceCollection services, IHostEnvironment environment) =>
        services.CritterStackDefaults(defaults =>
        {
            // Called from this library, the defaults would otherwise pin Lexora.Hosting as the application
            // assembly and the static mode would look for the pre-generated handlers in the wrong place.
            defaults.ApplicationAssembly = Assembly.Load(new AssemblyName(environment.ApplicationName));

            defaults.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
            defaults.Development.ResourceAutoCreate = AutoCreate.None;

            defaults.Production.GeneratedCodeMode = TypeLoadMode.Static;
            defaults.Production.ResourceAutoCreate = AutoCreate.None;
            defaults.Production.AssertAllPreGeneratedTypesExist = true;
        });
}
