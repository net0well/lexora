using System.Security.Claims;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;

namespace Lexora.Infrastructure.IntegrationTests.Identity;

public sealed class CurrentUserTests
{
    [Fact]
    public void CurrentUser_AuthenticatedRequest_ExposesIdAndRole()
    {
        var userId = Guid.CreateVersion7();

        var currentUser = For(new ClaimsIdentity(
            [new Claim(ClaimNames.Subject, userId.ToString()), new Claim(ClaimNames.Role, "Member")],
            AuthenticationSchemes.Bearer));

        currentUser.IsAuthenticated.ShouldBeTrue();
        currentUser.UserId.ShouldBe(userId);
        currentUser.Role.ShouldBe(UserRole.Member);
    }

    [Fact]
    public void CurrentUser_AnonymousRequest_IsNotAuthenticatedAndHasNoId()
    {
        var currentUser = For(new ClaimsIdentity([new Claim(ClaimNames.Subject, Guid.CreateVersion7().ToString())]));

        currentUser.IsAuthenticated.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => currentUser.UserId);
        Should.Throw<InvalidOperationException>(() => currentUser.Role);
    }

    [Fact]
    public void CurrentUser_WithoutHttpContext_IsNotAuthenticated()
    {
        var currentUser = new CurrentUser(new HttpContextAccessor());

        currentUser.IsAuthenticated.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => currentUser.UserId);
    }

    private static CurrentUser For(ClaimsIdentity identity) =>
        new(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } });
}
