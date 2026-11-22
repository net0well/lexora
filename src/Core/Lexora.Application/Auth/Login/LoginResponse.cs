namespace Lexora.Application.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUserResponse User,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
