using System.Net;
using System.Net.Http.Headers;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Ai.ListAiProfiles;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Ai;

public sealed class AiProfilesControllerTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task List_Viewer_SeesEveryChatProfileWithItsAvailabilityAndTheEmbeddingSpace()
    {
        var viewer = await Api.SeedUserAsync(TestUsers.ViewerEmail, UserRole.Viewer, TestUsers.Password);
        using var client = Api.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(viewer));

        using var response = await client.GetAsync("/api/v1/ai/profiles", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profiles = (await response.ReadEnvelopeAsync<ListAiProfilesResponse>(CancellationToken)).Data.ShouldNotBeNull();
        profiles.DefaultChatProfile.ShouldBe("fake");
        profiles.ChatProfiles[0].ShouldBe(new AiChatProfileResponse("fake", "fake", "fake-chat", Selectable: false, Available: true));
        profiles.ChatProfiles.Single(profile => profile.Name == "local")
            .ShouldBe(new AiChatProfileResponse("local", "ollama", "qwen2.5:7b", Selectable: true, Available: true));
        profiles.ChatProfiles.Where(profile => profile.Name is "claude" or "openai" or "groq")
            .ShouldAllBe(profile => !profile.Available && !profile.Selectable);
        profiles.Embedding.ShouldBe(new AiEmbeddingProfileResponse("fake", "fake-embedding", 1024));
    }
}
