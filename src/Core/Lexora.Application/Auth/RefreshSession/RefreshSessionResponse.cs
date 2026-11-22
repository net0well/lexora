namespace Lexora.Application.Auth.RefreshSession;

public sealed record RefreshSessionResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserResponse User,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
