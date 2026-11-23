using Lexora.Application.Conversations.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Conversations.DeleteConversation;

public static class DeleteConversationHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result> Handle(DeleteConversationCommand command, IConversationService service, CancellationToken cancellationToken) =>
        service.DeleteAsync(command.Id, cancellationToken);
}
