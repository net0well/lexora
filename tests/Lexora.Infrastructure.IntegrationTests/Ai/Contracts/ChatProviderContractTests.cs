using System.ClientModel;
using Lexora.Application.Abstractions.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.IntegrationTests.Ai.Contracts;

public sealed class ChatProviderContractTests
{
    private const string SystemPrompt = "You are part of an automated test. Reply with exactly the word the user asks for, in lowercase, and nothing else.";

    private static readonly ChatTurn[] _turns = [new(ChatTurnRole.User, "Reply with the word: lexora")];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("local")]
    [InlineData("groq")]
    [InlineData("claude")]
    [InlineData("openai")]
    public async Task CompleteAsync_RealProvider_FollowsTheInstructionAndReportsUsage(string profile)
    {
        using var host = new ProviderContractHost();
        await host.SkipUnlessChatProfileWorksAsync(profile);

        var completion = await host.Services.GetRequiredService<IChatModel>()
            .CompleteAsync(new ChatRequest(profile, SystemPrompt, _turns, MaxOutputTokens: 1024), CancellationToken);

        completion.Text.ShouldContain("lexora", Case.Insensitive);
        completion.Model.ShouldNotBeNullOrWhiteSpace();
        completion.InputTokens.ShouldNotBeNull().ShouldBeGreaterThan(0);
        completion.OutputTokens.ShouldNotBeNull().ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData("local")]
    [InlineData("groq")]
    [InlineData("claude")]
    [InlineData("openai")]
    public async Task StreamAsync_RealProvider_StreamsTheAnswerItFinishesWith(string profile)
    {
        using var host = new ProviderContractHost();
        await host.SkipUnlessChatProfileWorksAsync(profile);
        List<ChatStreamUpdate> updates = [];

        await foreach (var update in host.Services.GetRequiredService<IChatModel>()
            .StreamAsync(new ChatRequest(profile, SystemPrompt, _turns, MaxOutputTokens: 1024), CancellationToken))
        {
            updates.Add(update);
        }

        updates.Count(update => update.TextDelta is not null).ShouldBeGreaterThan(0);
        var completed = updates[^1].Completed.ShouldNotBeNull();
        completed.Text.ShouldBe(string.Concat(updates.Select(update => update.TextDelta)));
        completed.Text.ShouldContain("lexora", Case.Insensitive);
    }

    [Fact]
    public async Task CompleteAsync_GroqWithAWrongKey_IsAConfigurationErrorAndNotAnOutage()
    {
        using (var configured = new ProviderContractHost())
        {
            await configured.SkipUnlessChatProfileWorksAsync("groq");
        }

        using var host = new ProviderContractHost(("Ai:Chat:Profiles:groq:ApiKey", "gsk_not_a_real_key"));

        var failure = await Should.ThrowAsync<ClientResultException>(() =>
            host.Services.GetRequiredService<IChatModel>().CompleteAsync(new ChatRequest("groq", SystemPrompt, _turns), CancellationToken));

        failure.Status.ShouldBe(401);
    }
}
