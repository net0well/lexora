using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Lexora.Infrastructure.Identity;

public static class AuthenticationSchemes
{
    public const string Bearer = JwtBearerDefaults.AuthenticationScheme;
}
