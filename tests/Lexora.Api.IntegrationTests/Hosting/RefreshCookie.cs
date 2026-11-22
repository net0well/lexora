using System.Net.Http.Headers;

namespace Lexora.Api.IntegrationTests.Hosting;

public static class RefreshCookie
{
    public const string Name = "lexora_refresh";

    public static string? HeaderOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.SingleOrDefault(value => value.StartsWith(Name + "=", StringComparison.Ordinal))
            : null;

    public static string? ValueOf(HttpResponseMessage response)
    {
        var header = HeaderOf(response);

        return header?[(Name.Length + 1)..header.IndexOf(';', StringComparison.Ordinal)];
    }

    public static void Attach(HttpRequestHeaders headers, string value) => headers.Add("Cookie", $"{Name}={value}");
}
