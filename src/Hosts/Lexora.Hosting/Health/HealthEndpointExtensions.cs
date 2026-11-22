using Lexora.Shared.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

namespace Lexora.Hosting.Health;

public static class HealthEndpointExtensions
{
    public const string LivePath = "/health/live";
    public const string ReadyPath = "/health/ready";

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(LivePath, new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks(ReadyPath, new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthCheckTags.Ready) });

        return endpoints;
    }
}
