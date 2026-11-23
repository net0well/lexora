using System.Globalization;
using System.Text;

namespace Lexora.Application.Abstractions.Ai;

public sealed record ChatRequest(
    string? Profile,
    string SystemPrompt,
    IReadOnlyList<ChatTurn> Turns,
    float? Temperature = null,
    int? MaxOutputTokens = null)
{
    // Prompts must never reach the logs through an accidental ToString.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(CultureInfo.InvariantCulture, $"Profile = {Profile}, SystemPrompt = <{SystemPrompt.Length} chars>, Turns = {Turns.Count}, ");
        builder.Append(CultureInfo.InvariantCulture, $"Temperature = {Temperature}, MaxOutputTokens = {MaxOutputTokens}");

        return true;
    }
}
