using Microsoft.Extensions.Options;

namespace Lexora.Api.Auth;

public sealed class RefreshTokenCookie(IOptions<RefreshTokenCookieOptions> options, TimeProvider timeProvider)
{
    private readonly RefreshTokenCookieOptions _options = options.Value;

    public string? Read(HttpRequest request) => request.Cookies[_options.Name];

    public void Append(HttpResponse response, string refreshToken, DateTimeOffset expiresAt)
    {
        var cookie = NewCookie();
        cookie.Expires = expiresAt;
        cookie.MaxAge = expiresAt - timeProvider.GetUtcNow();

        response.Cookies.Append(_options.Name, refreshToken, cookie);
    }

    public void Delete(HttpResponse response) => response.Cookies.Delete(_options.Name, NewCookie());

    private CookieOptions NewCookie() => new()
    {
        HttpOnly = true,
        Secure = _options.Secure,
        SameSite = SameSiteMode.Strict,
        Path = _options.Path,
        IsEssential = true,
    };
}
