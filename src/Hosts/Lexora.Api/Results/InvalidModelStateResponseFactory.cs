using Lexora.Api.Contracts;
using Lexora.Api.ExceptionHandling;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Lexora.Api.Results;

public static class InvalidModelStateResponseFactory
{
    public const string Key = "request.malformed";

    public static IActionResult Create(ActionContext context)
    {
        if (context.ModelState.Values.SelectMany(entry => entry.Errors).Any(RequestSizeErrors.IsTooLarge))
        {
            return new ObjectResult(CommandResult.Fail([RequestSizeErrors.TooLarge])) { StatusCode = StatusCodes.Status413PayloadTooLarge };
        }

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
