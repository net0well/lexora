using Lexora.Domain.Users;
using Lexora.Shared.Domain;
using Lexora.Shared.Guards;
using Lexora.Shared.Results;

namespace Lexora.Domain.ApiKeys;

public sealed class ApiKey : AggregateRoot
{
    public const int NameMinLength = 3;
    public const int NameMaxLength = 80;
    public const int PrefixLength = 8;

    public static readonly TimeSpan UsageWriteInterval = TimeSpan.FromMinutes(5);

    private ApiKey()
    {
    }

    private ApiKey(
        string name,
        string prefix,
        string keyHash,
        UserRole role,
        Guid createdBy,
        DateTimeOffset? expiresAt,
        DateTimeOffset now)
        : base(Guid.CreateVersion7())
    {
        Name = name;
        Prefix = prefix;
        KeyHash = keyHash;
        Role = role;
        CreatedBy = createdBy;
        ExpiresAt = expiresAt;
        CreatedAt = now;
    }

    public string Name { get; private set; } = null!;

    public string Prefix { get; private set; } = null!;

    public string KeyHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? LastUsedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static Result<ApiKey> Create(
        string? name,
        string prefix,
        string keyHash,
        UserRole role,
        Guid createdBy,
        DateTimeOffset? expiresAt,
        DateTimeOffset now)
    {
        Guard.NotNullOrWhiteSpace(keyHash);
        Guard.NotEmpty(createdBy);

        if (Guard.NotNullOrWhiteSpace(prefix).Length != PrefixLength)
        {
            throw new ArgumentException($"The prefix must have exactly {PrefixLength} characters.", nameof(prefix));
        }

        var trimmedName = name?.Trim() ?? string.Empty;
        List<Notification> notifications = [];

        if (NameViolation(trimmedName) is { } nameViolation)
        {
            notifications.Add(nameViolation);
        }

        if (role is not (UserRole.Viewer or UserRole.Member))
        {
            notifications.Add(Notification.Validation("apiKey.role.invalid", "API keys can only have the Viewer or Member role."));
        }

        if (expiresAt <= now)
        {
            notifications.Add(Notification.Validation("apiKey.expiration.invalid", "The expiration must be in the future."));
        }

        return notifications.Count > 0
            ? Result.Failure<ApiKey>(notifications)
            : new ApiKey(trimmedName, prefix, keyHash, role, createdBy, expiresAt, now);
    }

    public bool IsValid(DateTimeOffset now) => RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;

    public void RegisterUse(DateTimeOffset now)
    {
        if (LastUsedAt is null || now - LastUsedAt.Value > UsageWriteInterval)
        {
            LastUsedAt = now;
        }
    }

    private static Notification? NameViolation(string name) => name.Length switch
    {
        0 => Notification.Validation("apiKey.name.required", "The name is required."),
        < NameMinLength => Notification.Validation("apiKey.name.tooShort", $"The name must have at least {NameMinLength} characters."),
        > NameMaxLength => Notification.Validation("apiKey.name.tooLong", $"The name cannot be longer than {NameMaxLength} characters."),
        _ => null,
    };
}
