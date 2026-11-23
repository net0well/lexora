using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Lexora.Api.ExceptionHandling;
using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Ai;
using Lexora.Application.Conversations.Streaming;
using Lexora.Shared.Results;

namespace Lexora.Api.Streaming;

public static partial class AnswerSseEvents
{
    // Once the first event is sent the status code is gone, so a failure is reported as one last error event.
    public static async IAsyncEnumerable<SseItem<string>> From(
        IAsyncEnumerator<AnswerStreamEvent> events,
        JsonSerializerOptions json,
        ILogger logger,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);

        await using (events)
        {
            yield return AnswerStreamEventMapper.ToSseItem(events.Current, json);

            while (true)
            {
                Notification? failure = null;
                var hasNext = false;

                try
                {
                    hasNext = await events.MoveNextAsync();
                }
                catch (AiProviderUnavailableException)
                {
                    failure = AiNotifications.Unavailable;
                }
                catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    LogStreamFailed(logger, exception);
                    failure = new Notification(UnhandledExceptionHandler.Key, "An unexpected error occurred while answering.", NotificationType.BusinessRule);
                }

                if (failure is not null)
                {
                    yield return AnswerStreamEventMapper.ToSseItem(new AnswerErrorEvent([failure]), json);
                    yield break;
                }

                if (!hasNext)
                {
                    yield break;
                }

                yield return AnswerStreamEventMapper.ToSseItem(events.Current, json);
            }
        }
    }

    [LoggerMessage(EventId = 5100, Level = LogLevel.Error, Message = "Answer stream failed after it started")]
    private static partial void LogStreamFailed(ILogger logger, Exception exception);
}
