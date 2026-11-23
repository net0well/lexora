using Lexora.Application.Conversations.AskQuestion;
using Lexora.Shared.Results;

namespace Lexora.Application.Conversations.Services;

public interface IAnswerService
{
    Task<Result<AskQuestionResponse>> AnswerAsync(AskQuestionCommand command, CancellationToken cancellationToken);
}
