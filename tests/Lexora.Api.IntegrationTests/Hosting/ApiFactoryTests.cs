using System.Net;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Hosting;

public sealed class ApiFactoryTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task HealthLive_RunningHost_RespondsOk()
    {
        using var client = Api.CreateApiClient();

        using var response = await client.GetAsync("/health/live", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SignIn_SeededUser_ReturnsAnAccessToken()
    {
        await Api.SeedUserAsync(TestUsers.AdminEmail, UserRole.Admin, TestUsers.Password);
        using var client = Api.CreateApiClient();

        var session = await client.SignInAsync(TestUsers.AdminEmail, TestUsers.Password, CancellationToken);

        session.AccessToken.ShouldNotBeNullOrWhiteSpace();
        session.User.Role.ShouldBe(UserRole.Admin);
    }

    [Fact]
    public async Task Swagger_OutsideDevelopment_IsNotExposed()
    {
        using var client = Api.CreateApiClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
