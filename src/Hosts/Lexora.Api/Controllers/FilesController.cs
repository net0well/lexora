using Asp.Versioning;
using Lexora.Api.Contracts;
using Lexora.Application.Abstractions.Storage;
using Lexora.Application.Documents;
using Lexora.Infrastructure.Storage.Local;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lexora.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/files")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class FilesController(LocalDownloadTickets tickets, IFileStorage storage) : ControllerBase
{
    [HttpGet("{ticket}")]
    public async Task<IActionResult> Download(string ticket, CancellationToken cancellationToken)
    {
        if (!tickets.TryRead(ticket, out var file))
        {
            return NotFoundEnvelope();
        }

        try
        {
            var content = await storage.OpenReadAsync(file.Key, cancellationToken);
            Response.Headers.XContentTypeOptions = "nosniff";

            return File(content, file.ContentType, file.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFoundEnvelope();
        }
    }

    private NotFoundObjectResult NotFoundEnvelope() => NotFound(CommandResult.Fail([DocumentNotifications.NotFound]));
}
