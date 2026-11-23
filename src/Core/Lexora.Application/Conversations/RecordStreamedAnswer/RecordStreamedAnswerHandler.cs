using Lexora.Application.Conversations.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Conversations.RecordStreamedAnswer;

public static class RecordStreamedAnswerHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<RecordStreamedAnswerResponse>> Handle(
        RecordStreamedAnswerCommand command,
        IAnswerService service,
        CancellationToken cancellationToken) =>
        service.RecordAsync(command, cancellationToken);
}
