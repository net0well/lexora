using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lexora.Api.Contracts;
using Lexora.Api.Contracts.Responses;

namespace Lexora.Api.IntegrationTests.Hosting;

public static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string path, T body, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(path, body, ApiJson.Options, cancellationToken);

    public static async Task<SessionResponse> SignInAsync(this HttpClient client, string email, string password, CancellationToken cancellationToken)
    {
        using var response = await client.PostJsonAsync("/api/v1/auth/login", new { email, password }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var session = (await response.ReadEnvelopeAsync<SessionResponse>(cancellationToken)).Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        return session;
    }

    public static async Task<CommandResult<T>> ReadEnvelopeAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken) =>
        (await response.Content.ReadFromJsonAsync<CommandResult<T>>(ApiJson.Options, cancellationToken))!;

    public static async Task<CommandResult> ReadEnvelopeAsync(this HttpResponseMessage response, CancellationToken cancellationToken) =>
        (await response.Content.ReadFromJsonAsync<CommandResult>(ApiJson.Options, cancellationToken))!;
}
