using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Authorization;

public sealed class AuthorizationMatrixTests(ApiFactory api) : ApiTest(api)
{
    private static readonly (string Method, string Path, UserRole MinimumRole)[] _endpoints =
    [
        ("GET", "/api/v1/users", UserRole.Admin),
        ("GET", "/api/v1/users/{id}", UserRole.Admin),
        ("POST", "/api/v1/users", UserRole.Admin),
        ("PUT", "/api/v1/users/{id}/role", UserRole.Admin),
        ("POST", "/api/v1/users/{id}/deactivate", UserRole.Admin),
        ("POST", "/api/v1/users/{id}/activate", UserRole.Admin),
        ("POST", "/api/v1/users/{id}/reset-password", UserRole.Admin),
        ("GET", "/api/v1/knowledge-bases", UserRole.Viewer),
        ("GET", "/api/v1/knowledge-bases/{id}", UserRole.Viewer),
        ("POST", "/api/v1/knowledge-bases", UserRole.Admin),
        ("PUT", "/api/v1/knowledge-bases/{id}", UserRole.Admin),
        ("POST", "/api/v1/knowledge-bases/{id}/archive", UserRole.Admin),
        ("POST", "/api/v1/knowledge-bases/{id}/documents", UserRole.Member),
        ("GET", "/api/v1/knowledge-bases/{id}/documents", UserRole.Viewer),
        ("GET", "/api/v1/documents/{id}", UserRole.Viewer),
        ("GET", "/api/v1/documents/{id}/chunks", UserRole.Viewer),
        ("GET", "/api/v1/documents/{id}/download", UserRole.Viewer),
        ("POST", "/api/v1/documents/{id}/reprocess", UserRole.Member),
        ("DELETE", "/api/v1/documents/{id}", UserRole.Member),
        ("GET", "/api/v1/auth/me", UserRole.Viewer),
        ("POST", "/api/v1/auth/logout", UserRole.Viewer),
        ("POST", "/api/v1/auth/change-password", UserRole.Viewer),
    ];

    public static TheoryData<string, string, UserRole, UserRole?> Matrix
    {
        get
        {
            var data = new TheoryData<string, string, UserRole, UserRole?>();

            foreach (var (method, path, minimumRole) in _endpoints)
            {
                foreach (var caller in new UserRole?[] { null, UserRole.Viewer, UserRole.Member, UserRole.Admin })
                {
                    data.Add(method, path, minimumRole, caller);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task Endpoint_Caller_GetsTheAccessOfItsRole(string method, string path, UserRole minimumRole, UserRole? caller)
    {
        using var client = Api.CreateApiClient();

        if (caller is { } callerRole)
        {
            var user = await Api.SeedUserAsync($"{callerRole.ToString().ToLowerInvariant()}@lexora.test", callerRole, TestUsers.Password);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(user));
        }

        using var request = new HttpRequestMessage(new HttpMethod(method), path.Replace("{id}", Guid.CreateVersion7().ToString(), StringComparison.Ordinal));

        if (path.EndsWith("/documents", StringComparison.Ordinal) && method == "POST")
        {
            request.Content = new MultipartFormDataContent { { new ByteArrayContent([1]), "file", "sample.pdf" } };
        }
        else if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { }, options: ApiJson.Options);
        }

        using var response = await client.SendAsync(request, CancellationToken);

        switch (caller)
        {
            case null:
                response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
                (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("auth.unauthenticated");
                break;
            case var role when role > minimumRole:
                response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
                (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("auth.forbidden");
                break;
            default:
                response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
                response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
                break;
        }
    }
}
