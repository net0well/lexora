using System.Net.ServerSentEvents;
using System.Text.Json;
using Lexora.Application.Conversations.Streaming;

namespace Lexora.Api.Streaming;

public static class AnswerStreamEventMapper
{
    public const string Delta = "delta";
    public const string Citations = "citations";
    public const string Done = "done";
    public const string Error = "error";

    public static SseItem<string> ToSseItem(AnswerStreamEvent answerEvent, JsonSerializerOptions json)
    {
        ArgumentNullException.ThrowIfNull(answerEvent);

        return new SseItem<string>(JsonSerializer.Serialize(answerEvent, answerEvent.GetType(), json), EventTypeOf(answerEvent));
    }

    public static string EventTypeOf(AnswerStreamEvent answerEvent) => answerEvent switch
    {
        AnswerDeltaEvent => Delta,
        AnswerCitationsEvent => Citations,
        AnswerDoneEvent => Done,
        AnswerErrorEvent => Error,
        _ => throw new ArgumentOutOfRangeException(nameof(answerEvent), answerEvent.GetType().Name, "Unknown answer stream event."),
    };
}
