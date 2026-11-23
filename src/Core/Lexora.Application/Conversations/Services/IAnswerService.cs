using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.RecordStreamedAnswer;
using Lexora.Shared.Results;

namespace Lexora.Application.Conversations.Services;

public interface IAnswerService
{
    Task<Result<AskQuestionResponse>> AnswerAsync(AskQuestionCommand command, CancellationToken cancellationToken);

    Task<Result<RecordStreamedAnswerResponse>> RecordAsync(RecordStreamedAnswerCommand command, CancellationToken cancellationToken);
}
