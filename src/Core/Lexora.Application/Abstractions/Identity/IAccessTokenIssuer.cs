using Lexora.Domain.Users;

namespace Lexora.Application.Abstractions.Identity;

public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}
