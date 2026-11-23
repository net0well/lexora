using Lexora.Application.Common.Options;
using Lexora.Application.KnowledgeBases;
using Lexora.Application.Retrieval;
using Lexora.Application.Retrieval.SearchKnowledgeBase;
using Lexora.Application.Retrieval.Services;
using Lexora.Domain.KnowledgeBases;
using Lexora.TestKit.Builders;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Lexora.Application.UnitTests.Retrieval;

public sealed class SearchKnowledgeBaseTests
{
    private readonly IKnowledgeBaseRepository _knowledgeBases = Substitute.For<IKnowledgeBaseRepository>();
    private readonly IRetrievalService _retrieval = Substitute.For<IRetrievalService>();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handle_ExistingBase_SearchesHybridWithTheConfiguredTopKByDefault()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().Build();
        _knowledgeBases.GetByIdAsync(knowledgeBase.Id, CancellationToken).Returns(knowledgeBase);

        var result = await Handle(new SearchKnowledgeBaseQuery(knowledgeBase.Id, "  raio de isolamento  "));

        result.IsSuccess.ShouldBeTrue();
        await _retrieval.Received(1).RetrieveAsync(knowledgeBase.Id, "raio de isolamento", SearchMode.Hybrid, 5, CancellationToken);
    }

    [Fact]
    public async Task Handle_UnknownBase_RespondsNotFoundWithoutSearching()
    {
        var result = await Handle(new SearchKnowledgeBaseQuery(Guid.CreateVersion7(), "raio"));

        result.Notifications.ShouldBe([KnowledgeBaseNotifications.NotFound]);
        await _retrieval.DidNotReceiveWithAnyArgs().RetrieveAsync(default, default!, default, default, CancellationToken);
    }

    [Theory]
    [InlineData("", null, null, "search.query.required")]
    [InlineData("raio", (SearchMode)9, null, "search.mode.invalid")]
    [InlineData("raio", null, 0, "search.topK.invalid")]
    [InlineData("raio", null, 51, "search.topK.invalid")]
    public void Validate_InvalidQuery_ReportsTheKey(string text, SearchMode? mode, int? topK, string expected) =>
        new SearchKnowledgeBaseValidator().Validate(new SearchKnowledgeBaseQuery(Guid.CreateVersion7(), text, mode, topK))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(expected);

    [Fact]
    public void Validate_QueryLongerThanTheLimit_IsRejected() =>
        new SearchKnowledgeBaseValidator().Validate(new SearchKnowledgeBaseQuery(Guid.CreateVersion7(), new string('a', 1_001)))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("search.query.tooLong");

    private Task<Lexora.Shared.Results.Result<SearchKnowledgeBaseResponse>> Handle(SearchKnowledgeBaseQuery query) =>
        SearchKnowledgeBaseHandler.Handle(query, _knowledgeBases, _retrieval, Options.Create(new RagOptions { TopK = 5 }), CancellationToken);
}
