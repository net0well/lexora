using Lexora.Api.Auth;
using Lexora.TestKit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Api.IntegrationTests.Auth;

public sealed class RefreshTokenCookieTests
{
    private readonly FakeTimeProvider _time = new(TestClock.Now);

    [Fact]
    public void Append_RefreshToken_WritesAHardenedCookieScopedToAuthRoutes()
    {
        var context = new DefaultHttpContext();

        NewCookie(secure: true).Append(context.Response, "refresh-value", _time.GetUtcNow().AddDays(7));

        var header = SetCookieHeader(context);
        header.ShouldStartWith("lexora_refresh=refresh-value;");
        header.ShouldContain("max-age=604800");
        header.ShouldContain("path=/api/v1/auth");
        header.ShouldContain("secure");
        header.ShouldContain("samesite=strict");
        header.ShouldContain("httponly");
    }

    [Fact]
    public void Append_InsecureEnvironment_OmitsTheSecureFlag()
    {
        var context = new DefaultHttpContext();

        NewCookie(secure: false).Append(context.Response, "refresh-value", _time.GetUtcNow().AddDays(7));

        SetCookieHeader(context).ShouldNotContain("secure");
    }

    [Fact]
    public void Delete_ExistingCookie_ExpiresItOnTheSamePath()
    {
        var context = new DefaultHttpContext();

        NewCookie(secure: true).Delete(context.Response);

        var header = SetCookieHeader(context);
        header.ShouldStartWith("lexora_refresh=;");
        header.ShouldContain("expires=Thu, 01 Jan 1970");
        header.ShouldContain("path=/api/v1/auth");
    }

    [Fact]
    public void Read_RequestWithCookie_ReturnsItsValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "other=1; lexora_refresh=refresh-value";

        NewCookie(secure: true).Read(context.Request).ShouldBe("refresh-value");
    }

    [Fact]
    public void Read_RequestWithoutCookie_ReturnsNull() =>
        NewCookie(secure: true).Read(new DefaultHttpContext().Request).ShouldBeNull();

    private RefreshTokenCookie NewCookie(bool secure) =>
        new(Options.Create(new RefreshTokenCookieOptions { Secure = secure }), _time);

    private static string SetCookieHeader(HttpContext context) => context.Response.Headers.SetCookie.ToString();
}
