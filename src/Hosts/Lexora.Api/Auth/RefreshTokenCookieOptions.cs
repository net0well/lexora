namespace Lexora.Api.Auth;

public sealed class RefreshTokenCookieOptions
{
    public string Name { get; set; } = "lexora_refresh";

    public string Path { get; set; } = "/api/v1/auth";

    public bool Secure { get; set; } = true;
}
