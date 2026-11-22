using Lexora.Api.Contracts.Requests;
using Lexora.Api.Mappers;
using Lexora.Application.KnowledgeBases.CreateKnowledgeBase;
using Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;

namespace Lexora.Api.IntegrationTests.Mappers;

public sealed class KnowledgeBaseRequestMapperTests
{
    [Fact]
    public void ToCommand_CreateKnowledgeBase_CopiesEveryField() =>
        new CreateKnowledgeBaseRequest("Safety", "Manuals", 800, 120).ToCommand()
            .ShouldBe(new CreateKnowledgeBaseCommand("Safety", "Manuals", 800, 120));

    [Fact]
    public void ToCommand_UpdateKnowledgeBase_TakesTheIdFromTheRoute()
    {
        var id = Guid.CreateVersion7();

        new UpdateKnowledgeBaseRequest("Safety", null, null, null).ToCommand(id)
            .ShouldBe(new UpdateKnowledgeBaseCommand(id, "Safety", null, null, null));
    }
}
