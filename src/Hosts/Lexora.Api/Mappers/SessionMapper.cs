using Lexora.Api.Contracts.Responses;
using Lexora.Application.Auth.Login;
using Lexora.Application.Auth.RefreshSession;

namespace Lexora.Api.Mappers;

public static class SessionMapper
{
    public static SessionResponse ToSession(this LoginResponse response) => new(response.AccessToken, response.ExpiresAt, response.User);

    public static SessionResponse ToSession(this RefreshSessionResponse response) => new(response.AccessToken, response.ExpiresAt, response.User);
}
