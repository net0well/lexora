using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.Streaming;

namespace Lexora.Application.Conversations.Services;

public interface IAnswerStreamingService
{
    IAsyncEnumerable<AnswerStreamEvent> StreamAsync(AskQuestionCommand command, CancellationToken cancellationToken);
}
