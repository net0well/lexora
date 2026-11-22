namespace Lexora.Application.Auth.SeedAdmin;

public sealed class SeedAdminOptions
{
    public const string SectionName = "Seed:Admin";

    public string? Email { get; init; }

    public string? Name { get; init; }

    public string? Password { get; init; }
}
