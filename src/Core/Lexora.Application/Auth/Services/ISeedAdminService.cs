using Lexora.Application.Auth.SeedAdmin;
using Lexora.Shared.Results;

namespace Lexora.Application.Auth.Services;

public interface ISeedAdminService
{
    Task<Result<SeedAdminResponse>> SeedAsync(SeedAdminCommand command, CancellationToken cancellationToken);
}
