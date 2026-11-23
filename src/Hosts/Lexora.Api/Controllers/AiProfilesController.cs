using Asp.Versioning;
using Lexora.Api.Contracts;
using Lexora.Api.Results;
using Lexora.Application.Ai.ListAiProfiles;
using Lexora.Infrastructure.Identity;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Lexora.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/ai/profiles")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.RequireViewer)]
[ProducesResponseType<CommandResult>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<CommandResult>(StatusCodes.Status403Forbidden)]
public sealed class AiProfilesController(IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CommandResult<ListAiProfilesResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await bus.InvokeAsync<Result<ListAiProfilesResponse>>(new ListAiProfilesQuery(), cancellationToken)).ToActionResult();
}
