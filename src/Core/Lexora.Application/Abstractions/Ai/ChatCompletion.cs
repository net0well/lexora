using System.Globalization;
using System.Text;

namespace Lexora.Application.Abstractions.Ai;

public sealed record ChatCompletion(
    string Text,
    string Provider,
    string Model,
    int? InputTokens,
    int? OutputTokens,
    TimeSpan Latency)
{
    // Answers must never reach the logs through an accidental ToString.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(CultureInfo.InvariantCulture, $"Text = <{Text.Length} chars>, Provider = {Provider}, Model = {Model}, ");
        builder.Append(CultureInfo.InvariantCulture, $"InputTokens = {InputTokens}, OutputTokens = {OutputTokens}, Latency = {Latency}");

        return true;
    }
}
