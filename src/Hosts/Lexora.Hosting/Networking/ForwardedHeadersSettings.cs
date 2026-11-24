namespace Lexora.Hosting.Networking;

public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "Hosting:ForwardedHeaders";

    public string[] KnownProxies { get; init; } = [];

    public string[] KnownNetworks { get; init; } = [];
}
