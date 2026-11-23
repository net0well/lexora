using Lexora.Application.Common.Options;
using Lexora.Application.Conversations.Prompts;
using Lexora.Application.Conversations.Services;
using Lexora.Application.Retrieval;
using Lexora.Application.Retrieval.Fusion;
using Lexora.Application.Retrieval.Services;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class AnswerContextBuilderTests
{
    [Fact]
    public async Task BuildAsync_NoExcerptFitsTheBudget_HasNoPromptSoTheModelIsNotCalled()
    {
        var retrieval = Substitute.For<IRetrievalService>();
        var longExcerpt = new SearchHit(Guid.CreateVersion7(), Guid.CreateVersion7(), "manual.pdf", string.Join(' ', Enumerable.Repeat("palavra", 60)), 1, null, null, 0.03, SearchMatch.Both);
        retrieval.RetrieveAsync(default, default!, default, default, TestContext.Current.CancellationToken).ReturnsForAnyArgs([longExcerpt]);
        var options = Options.Create(new RagOptions { MaxContextTokens = 500 });
        var builder = new AnswerContextBuilder(
            retrieval,
            new RagPromptBuilder(new WordTokenCounter(), Options.Create(new RagOptions { MaxContextTokens = 20 })),
            new FollowUpQuestionRewriter(new FakeChatModel()),
            options);

        var context = await builder.BuildAsync(Guid.CreateVersion7(), "Qual o raio?", [], profile: null, TestContext.Current.CancellationToken);

        context.Prompt.ShouldBeNull();
    }
}
