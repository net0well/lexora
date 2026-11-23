using Lexora.Application.Conversations.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Conversations.StartConversation;

public static class StartConversationHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<StartConversationResponse>> Handle(StartConversationCommand command, IConversationService service, CancellationToken cancellationToken) =>
        service.StartAsync(command, cancellationToken);
}
