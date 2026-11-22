using Lexora.Api.Contracts;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Lexora.Api.Results;

public static class InvalidModelStateResponseFactory
{
    public const string Key = "request.malformed";

    public static IActionResult Create(ActionContext context)
    {
        var notifications = context.ModelState
            .Where(entry => entry.Value is not null)
            .SelectMany(entry => entry.Value!.Errors.Select(error => Notification.Validation(
                Key,
                string.IsNullOrWhiteSpace(entry.Key) ? "The request body is missing or malformed." : $"The field '{entry.Key}' is malformed.")))
            .DistinctBy(notification => notification.Message)
            .DefaultIfEmpty(Notification.Validation(Key, "The request body is missing or malformed."));

        return new BadRequestObjectResult(CommandResult.Fail(notifications));
    }
}
