using System.Text;
using Lexora.Application.Auth;

namespace Lexora.Api.Contracts.Responses;

public sealed record SessionResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthenticatedUserResponse User)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("User = ").Append(User).Append(", ExpiresAt = ").Append(ExpiresAt);

        return true;
    }
}
