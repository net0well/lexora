using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Common.Paging;
using Lexora.Application.Users;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Users;

public sealed class UsersControllerTests(ApiFactory api) : ApiTest(api)
{
    private const string BasePath = "/api/v1/users";

    [Fact]
    public async Task Create_NewUser_RespondsCreatedAndTheUserCanSignIn()
    {
        using var client = await AdminClientAsync();

        using var response = await client.PostJsonAsync(
            BasePath,
            new { email = "Bruno.Lima@Lexora.test", name = "Bruno Lima", role = "member", password = TestUsers.Password },
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await response.ReadEnvelopeAsync<UserResponse>(CancellationToken)).Data.ShouldNotBeNull();
        created.Email.ShouldBe("bruno.lima@lexora.test");
        created.Role.ShouldBe(UserRole.Member);
        response.Headers.Location.ShouldBe(new Uri($"{BasePath}/{created.Id}", UriKind.Relative));
        (await LoginStatusAsync("bruno.lima@lexora.test", TestUsers.Password)).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_EmailAlreadyUsed_RespondsConflict()
    {
        using var client = await AdminClientAsync();
        await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);

        using var response = await client.PostJsonAsync(
            BasePath,
            new { email = TestUsers.MemberEmail, name = "Someone", role = "viewer", password = TestUsers.Password },
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("user.email.duplicate");
    }

    [Fact]
    public async Task List_SearchAndRole_FilterTheUsers()
    {
        using var client = await AdminClientAsync();
        await Api.SeedUserAsync("ana.souza@lexora.test", UserRole.Member, TestUsers.Password);
        await Api.SeedUserAsync("ana.lima@lexora.test", UserRole.Viewer, TestUsers.Password);

        using var response = await client.GetAsync($"{BasePath}?search=ANA&role=viewer", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = (await response.ReadEnvelopeAsync<PagedResponse<UserResponse>>(CancellationToken)).Data.ShouldNotBeNull();
        page.Items.ShouldHaveSingleItem().Email.ShouldBe("ana.lima@lexora.test");
    }

    [Fact]
    public async Task ChangeRole_LastActiveAdmin_RespondsUnprocessable()
    {
        var admin = await Api.SeedUserAsync(TestUsers.AdminEmail, UserRole.Admin, TestUsers.Password);
        using var client = ClientFor(admin);

        using var response = await client.PutAsJsonAsync($"{BasePath}/{admin.Id}/role", new { role = "member" }, ApiJson.Options, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("user.lastAdmin");
    }

    [Fact]
    public async Task Deactivate_OwnAccount_RespondsUnprocessable()
    {
        await Api.SeedUserAsync("second.admin@lexora.test", UserRole.Admin, TestUsers.Password);
        var admin = await Api.SeedUserAsync(TestUsers.AdminEmail, UserRole.Admin, TestUsers.Password);
        using var client = ClientFor(admin);

        using var response = await client.PostAsync($"{BasePath}/{admin.Id}/deactivate", content: null, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("user.selfDeactivation");
    }

    [Fact]
    public async Task DeactivateThenActivate_Member_BlocksAndRestoresTheSignIn()
    {
        using var client = await AdminClientAsync();
        var member = await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);

        using var deactivate = await client.PostAsync($"{BasePath}/{member.Id}/deactivate", content: null, CancellationToken);
        var whileInactive = await LoginStatusAsync(TestUsers.MemberEmail, TestUsers.Password);
        using var activate = await client.PostAsync($"{BasePath}/{member.Id}/activate", content: null, CancellationToken);
        var afterActivation = await LoginStatusAsync(TestUsers.MemberEmail, TestUsers.Password);

        (await deactivate.ReadEnvelopeAsync<UserResponse>(CancellationToken)).Data.ShouldNotBeNull().Status.ShouldBe(UserStatus.Inactive);
        whileInactive.ShouldBe(HttpStatusCode.Unauthorized);
        activate.StatusCode.ShouldBe(HttpStatusCode.OK);
        afterActivation.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_StrongPassword_ReplacesTheOldOne()
    {
        using var client = await AdminClientAsync();
        var member = await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);

        using var response = await client.PostJsonAsync($"{BasePath}/{member.Id}/reset-password", new { newPassword = "Reset-Password-2026" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await LoginStatusAsync(TestUsers.MemberEmail, TestUsers.Password)).ShouldBe(HttpStatusCode.Unauthorized);
        (await LoginStatusAsync(TestUsers.MemberEmail, "Reset-Password-2026")).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_UnknownUser_RespondsNotFound()
    {
        using var client = await AdminClientAsync();

        using var response = await client.GetAsync($"{BasePath}/{Guid.CreateVersion7()}", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("user.notFound");
    }

    private async Task<HttpClient> AdminClientAsync() =>
        ClientFor(await Api.SeedUserAsync(TestUsers.AdminEmail, UserRole.Admin, TestUsers.Password));

    private HttpClient ClientFor(User user)
    {
        var client = Api.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(user));

        return client;
    }

    private async Task<HttpStatusCode> LoginStatusAsync(string email, string password)
    {
        using var client = Api.CreateApiClient();
        using var response = await client.PostJsonAsync("/api/v1/auth/login", new { email, password }, CancellationToken);

        return response.StatusCode;
    }
}
