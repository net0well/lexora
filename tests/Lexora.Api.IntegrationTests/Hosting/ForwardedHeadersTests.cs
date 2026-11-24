using System.Net;
using Lexora.Hosting;
using Lexora.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Lexora.Api.IntegrationTests.Hosting;

public sealed class ForwardedHeadersTests
{
    private const string Client = "203.0.113.7";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("10.231.0.10", Client)]
    [InlineData("198.51.100.9", "198.51.100.9")]
    public async Task ClientAddress_FromATrustedProxyOnly_ComesFromTheForwardedHeader(string connection, string expected)
    {
        await using var app = await StartAsync(IPAddress.Parse(connection), ("Hosting:ForwardedHeaders:KnownProxies:0", "10.231.0.10"));

        (await RemoteAddressAsync(app)).ShouldBe(expected);
    }

    [Fact]
    public async Task ClientAddress_FromATrustedNetwork_ComesFromTheForwardedHeader()
    {
        await using var app = await StartAsync(IPAddress.Parse("10.231.0.25"), ("Hosting:ForwardedHeaders:KnownNetworks:0", "10.231.0.0/24"));

        (await RemoteAddressAsync(app)).ShouldBe(Client);
    }

    [Fact]
    public async Task ClientAddress_WithoutTrustedProxies_IgnoresTheForwardedHeader()
    {
        await using var app = await StartAsync(IPAddress.Parse("10.231.0.10"));

        (await RemoteAddressAsync(app)).ShouldBe("10.231.0.10");
    }

    [Theory]
    [InlineData("Hosting:ForwardedHeaders:KnownProxies:0", "not-an-ip")]
    [InlineData("Hosting:ForwardedHeaders:KnownNetworks:0", "10.231.0.0/99")]
    public void AddHostingDefaults_InvalidTrustedAddress_StopsTheHost(string key, string value)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(Settings((key, value)));

        Should.Throw<InvalidOperationException>(() => builder.Services.AddHostingDefaults(builder.Configuration, builder.Environment))
            .Message.ShouldContain(value);
    }

    private static Dictionary<string, string?> Settings(params (string Key, string Value)[] settings)
    {
        var values = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ConnectionStringNames.Database}"] = "Host=localhost;Database=lexora",
            [$"ConnectionStrings:{ConnectionStringNames.RabbitMq}"] = "amqp://localhost:5672",
        };

        foreach (var (key, value) in settings)
        {
            values[key] = value;
        }

        return values;
    }

    private static async Task<WebApplication> StartAsync(IPAddress connection, params (string Key, string Value)[] settings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(Settings(settings));
        builder.Services.AddHostingDefaults(builder.Configuration, builder.Environment);

        var app = builder.Build();
        app.Use((context, next) =>
        {
            context.Connection.RemoteIpAddress = connection;
            return next(context);
        });
        app.UseForwardedHeaders();
        app.MapGet("/client", (HttpContext context) => context.Connection.RemoteIpAddress?.ToString());
        await app.StartAsync(CancellationToken);

        return app;
    }

    private static async Task<string> RemoteAddressAsync(WebApplication app)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/client");
        request.Headers.Add("X-Forwarded-For", Client);
        using var response = await app.GetTestClient().SendAsync(request, CancellationToken);

        return await response.Content.ReadAsStringAsync(CancellationToken);
    }
}
