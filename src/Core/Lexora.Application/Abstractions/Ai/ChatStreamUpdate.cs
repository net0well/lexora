using System.Globalization;
using System.Text;

namespace Lexora.Application.Abstractions.Ai;

public sealed record ChatStreamUpdate(string? TextDelta, ChatCompletion? Completed)
{
    public static ChatStreamUpdate Delta(string text) => new(text, null);

    public static ChatStreamUpdate Finished(ChatCompletion completion) => new(null, completion);

    // Answers must never reach the logs through an accidental ToString.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(CultureInfo.InvariantCulture, $"TextDelta = <{TextDelta?.Length ?? 0} chars>, Completed = {Completed}");

        return true;
    }
}
