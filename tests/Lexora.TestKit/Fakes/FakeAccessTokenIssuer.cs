using Lexora.Application.Abstractions.Identity;
using Lexora.Domain.Users;

namespace Lexora.TestKit.Fakes;

public sealed class FakeAccessTokenIssuer(TimeProvider timeProvider) : IAccessTokenIssuer
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public AccessToken Issue(User user) => new($"access-token:{user.Id}", timeProvider.GetUtcNow() + Lifetime);
}
