using Lexora.Application.Common.Paging;
using Lexora.Application.Common.Validation;
using Lexora.Application.KnowledgeBases;
using Lexora.Application.KnowledgeBases.Abstractions;
using Lexora.Application.KnowledgeBases.GetKnowledgeBase;
using Lexora.Application.KnowledgeBases.ListKnowledgeBases;
using Lexora.Domain.KnowledgeBases;
using Lexora.TestKit;
using NSubstitute;

namespace Lexora.Application.UnitTests.KnowledgeBases;

public sealed class KnowledgeBaseQueryHandlerTests
{
    private static readonly GetKnowledgeBaseResponse _knowledgeBase = new(
        Guid.CreateVersion7(), "Safety Procedures", null, KnowledgeBaseStatus.Active, 800, 120, 3, TestClock.Now, TestClock.Now);

    private readonly IKnowledgeBaseQueries _queries = Substitute.For<IKnowledgeBaseQueries>();

    [Fact]
    public async Task GetKnowledgeBase_Existing_ReturnsIt()
    {
        _queries.GetByIdAsync(_knowledgeBase.Id, Arg.Any<CancellationToken>()).Returns(_knowledgeBase);

        (await GetKnowledgeBaseHandler.Handle(new GetKnowledgeBaseQuery(_knowledgeBase.Id), _queries, TestContext.Current.CancellationToken))
            .Value.ShouldBe(_knowledgeBase);
    }

    [Fact]
    public async Task GetKnowledgeBase_Unknown_FailsAsNotFound() =>
        (await GetKnowledgeBaseHandler.Handle(new GetKnowledgeBaseQuery(Guid.CreateVersion7()), _queries, TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([KnowledgeBaseNotifications.NotFound]);

    [Fact]
    public async Task ListKnowledgeBases_Query_ReturnsThePageFromTheQueries()
    {
        var query = new ListKnowledgeBasesQuery(KnowledgeBaseStatus.Active);
        var item = new KnowledgeBaseListItemResponse(_knowledgeBase.Id, _knowledgeBase.Name, null, KnowledgeBaseStatus.Active, 3, TestClock.Now);
        var page = PagedResponse.Create<KnowledgeBaseListItemResponse>([item], query.Paging, totalItems: 1);
        _queries.ListAsync(query, Arg.Any<CancellationToken>()).Returns(page);

        (await ListKnowledgeBasesHandler.Handle(query, _queries, TestContext.Current.CancellationToken)).Value.ShouldBe(page);
    }

    [Theory]
    [InlineData(0, 20, PagingRuleExtensions.InvalidPageKey)]
    [InlineData(1, 101, PagingRuleExtensions.InvalidPageSizeKey)]
    public void ListKnowledgeBasesValidator_PagingOutOfRange_ReportsTheKey(int page, int pageSize, string expectedKey) =>
        new ListKnowledgeBasesValidator().Validate(new ListKnowledgeBasesQuery(null, page, pageSize))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(expectedKey);

    [Fact]
    public void ListKnowledgeBasesValidator_UndefinedStatus_IsInvalid() =>
        new ListKnowledgeBasesValidator().Validate(new ListKnowledgeBasesQuery((KnowledgeBaseStatus)42))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("knowledgeBase.status.invalid");
}
