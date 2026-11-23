using System.Globalization;
using System.Text;

namespace Lexora.Application.Abstractions.Ai;

public sealed record ChatTurn(ChatTurnRole Role, string Content)
{
    // Conversation content must never reach the logs through an accidental ToString.
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(CultureInfo.InvariantCulture, $"Role = {Role}, Content = <{Content.Length} chars>");

        return true;
    }
}
