using JasperFx;
using JasperFx.CodeGeneration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Hosting.Wolverine;

internal static class CodeGenerationDefaults
{
    public static void Apply(IServiceCollection services) =>
        services.CritterStackDefaults(defaults =>
        {
            defaults.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
            defaults.Development.ResourceAutoCreate = AutoCreate.None;

            defaults.Production.GeneratedCodeMode = TypeLoadMode.Static;
            defaults.Production.ResourceAutoCreate = AutoCreate.None;
            defaults.Production.AssertAllPreGeneratedTypesExist = true;
        });
}
