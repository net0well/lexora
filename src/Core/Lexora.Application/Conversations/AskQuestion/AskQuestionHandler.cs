using Lexora.Application.Conversations.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Conversations.AskQuestion;

public static class AskQuestionHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<AskQuestionResponse>> Handle(AskQuestionCommand command, IAnswerService service, CancellationToken cancellationToken) =>
        service.AnswerAsync(command, cancellationToken);
}
