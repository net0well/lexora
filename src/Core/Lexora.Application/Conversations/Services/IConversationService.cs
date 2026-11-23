using Lexora.Application.Conversations.StartConversation;
using Lexora.Shared.Results;

namespace Lexora.Application.Conversations.Services;

public interface IConversationService
{
    Task<Result<StartConversationResponse>> StartAsync(StartConversationCommand command, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid conversationId, CancellationToken cancellationToken);
}
