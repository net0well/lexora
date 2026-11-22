using Lexora.Application.Auth.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Auth.RefreshSession;

public static class RefreshSessionHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<RefreshSessionResponse>> Handle(
        RefreshSessionCommand command,
        IAuthService service,
        CancellationToken cancellationToken) =>
        service.RefreshAsync(command, cancellationToken);
}
