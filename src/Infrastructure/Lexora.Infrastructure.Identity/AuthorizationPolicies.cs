namespace Lexora.Infrastructure.Identity;

public static class AuthorizationPolicies
{
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireMember = nameof(RequireMember);
    public const string RequireViewer = nameof(RequireViewer);
}
