using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Hosting.Networking;

internal static class ForwardedHeadersConfiguration
{
    // Only the reverse proxy may set the client address: trusting X-Forwarded-For from anyone would let
    // a caller pick the IP that rate limiting and audit logs see.
    public static void Apply(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(ForwardedHeadersSettings.SectionName).Get<ForwardedHeadersSettings>()
            ?? new ForwardedHeadersSettings();
        var proxies = settings.KnownProxies.Select(ParseAddress).ToList();
        var networks = settings.KnownNetworks.Select(ParseNetwork).ToList();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            foreach (var proxy in proxies)
            {
                options.KnownProxies.Add(proxy);
            }

            foreach (var network in networks)
            {
                options.KnownIPNetworks.Add(network);
            }
        });
    }

    private static IPAddress ParseAddress(string value) =>
        IPAddress.TryParse(value, out var address)
            ? address
            : throw new InvalidOperationException($"'{ForwardedHeadersSettings.SectionName}:KnownProxies' has an invalid IP address: '{value}'.");

    private static System.Net.IPNetwork ParseNetwork(string value) =>
        System.Net.IPNetwork.TryParse(value, out var network)
            ? network
            : throw new InvalidOperationException($"'{ForwardedHeadersSettings.SectionName}:KnownNetworks' has an invalid network: '{value}'.");
}
