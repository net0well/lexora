using Lexora.Api.Contracts;
using Lexora.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Lexora.Api.Results;

public static class ResultActionExtensions
{
    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new OkObjectResult(CommandResult.Ok()) : Failure(result);

    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess ? new OkObjectResult(CommandResult.Ok(result.Value)) : Failure(result);

    public static IActionResult ToActionResult<T, TResponse>(this Result<T> result, Func<T, TResponse> map) =>
        result.IsSuccess ? new OkObjectResult(CommandResult.Ok(map(result.Value))) : Failure(result);

    public static IActionResult ToCreatedResult<T>(this Result<T> result, Func<T, string?> location) =>
        result.IsSuccess ? new CreatedResult(location(result.Value), CommandResult.Ok(result.Value)) : Failure(result);

    public static IActionResult ToAcceptedResult<T>(this Result<T> result, Func<T, string?> location) =>
        result.IsSuccess ? new AcceptedResult(location(result.Value), CommandResult.Ok(result.Value)) : Failure(result);

    private static ObjectResult Failure(Result result) =>
        new(CommandResult.Fail(result.Notifications)) { StatusCode = NotificationStatusCodeMapper.ToStatusCode(result.Notifications) };
}
