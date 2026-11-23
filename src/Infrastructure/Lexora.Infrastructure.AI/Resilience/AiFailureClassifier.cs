using System.ClientModel;
using System.Globalization;
using System.Net;
using Anthropic.Exceptions;
using Polly.CircuitBreaker;

namespace Lexora.Infrastructure.AI.Resilience;

public static class AiFailureClassifier
{
    public static bool IsUnavailable(Exception exception, CancellationToken cancellationToken) =>
        exception is BrokenCircuitException || IsTransient(exception, cancellationToken);

    public static bool IsTransient(Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            // A missing status code means the provider could not be reached at all, which is as transient as a 5xx.
            HttpRequestException http => http.StatusCode is not { } status || IsTransientStatus((int)status),
            ClientResultException client => client.Status == 0 || IsTransientStatus(client.Status),
            AnthropicApiException anthropic => IsTransientStatus((int)anthropic.StatusCode),
            TaskCanceledException or TimeoutException => !cancellationToken.IsCancellationRequested,
            _ => false,
        };
    }

    public static TimeSpan? RetryAfterOf(Exception exception) => exception switch
    {
        ClientResultException client when client.GetRawResponse()?.Headers.TryGetValue("Retry-After", out var value) == true => Parse(value),
        _ => null,
    };

    private static bool IsTransientStatus(int status) =>
        status is (int)HttpStatusCode.RequestTimeout or (int)HttpStatusCode.TooManyRequests or >= 500;

    private static TimeSpan? Parse(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0 ? TimeSpan.FromSeconds(seconds) : null;
}
