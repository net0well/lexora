using Lexora.Domain.ApiKeys;
using Lexora.Domain.Users;
using Lexora.Shared.Results;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Domain.UnitTests.ApiKeys;

public sealed class ApiKeyTests
{
    private const string Prefix = "a1b2c3d4";
    private const string KeyHash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

    private static readonly Guid _adminId = Guid.CreateVersion7();

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 21, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_ValidInput_CreatesUnusedKey()
    {
        var result = ApiKey.Create("  Reporting agent ", Prefix, KeyHash, UserRole.Viewer, _adminId, null, _time.GetUtcNow());

        result.IsSuccess.ShouldBeTrue();
        var apiKey = result.Value;
        apiKey.Name.ShouldBe("Reporting agent");
        apiKey.Prefix.ShouldBe(Prefix);
        apiKey.Role.ShouldBe(UserRole.Viewer);
        apiKey.CreatedBy.ShouldBe(_adminId);
        apiKey.CreatedAt.ShouldBe(_time.GetUtcNow());
        apiKey.LastUsedAt.ShouldBeNull();
        apiKey.RevokedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData((UserRole)99)]
    public void Create_RoleOtherThanViewerOrMember_FailsWithRoleInvalid(UserRole role) =>
        Create(role: role).Notifications.Single().Key.ShouldBe("apiKey.role.invalid");

    [Theory]
    [InlineData(null, "apiKey.name.required")]
    [InlineData("   ", "apiKey.name.required")]
    [InlineData("ab", "apiKey.name.tooShort")]
    public void Create_InvalidName_FailsWithMatchingKey(string? name, string expectedKey) =>
        Create(name: name).Notifications.Single().Key.ShouldBe(expectedKey);

    [Fact]
    public void Create_NameOverMaximum_FailsAsTooLong() =>
        Create(name: new string('a', ApiKey.NameMaxLength + 1)).Notifications.Single().Key.ShouldBe("apiKey.name.tooLong");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ExpirationNotInFuture_FailsWithExpirationInvalid(int minutesFromNow) =>
        Create(expiresAt: _time.GetUtcNow().AddMinutes(minutesFromNow)).Notifications.Single().Key.ShouldBe("apiKey.expiration.invalid");

    [Fact]
    public void Create_SeveralViolations_ReportsAllOfThem() =>
        Create(name: "", role: UserRole.Admin, expiresAt: _time.GetUtcNow())
            .Notifications.Select(notification => notification.Key)
            .ShouldBe(["apiKey.name.required", "apiKey.role.invalid", "apiKey.expiration.invalid"]);

    [Theory]
    [InlineData("short")]
    [InlineData("toolongprefix")]
    public void Create_PrefixWithWrongLength_Throws(string prefix) =>
        Should.Throw<ArgumentException>(() => ApiKey.Create("Agent", prefix, KeyHash, UserRole.Member, _adminId, null, _time.GetUtcNow()));

    [Fact]
    public void IsValid_WithoutExpiration_StaysValid()
    {
        var apiKey = Create().Value;

        _time.Advance(TimeSpan.FromDays(3650));

        apiKey.IsValid(_time.GetUtcNow()).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_AtExpiration_ReturnsFalse()
    {
        var apiKey = Create(expiresAt: _time.GetUtcNow().AddDays(30)).Value;

        _time.Advance(TimeSpan.FromDays(30));

        apiKey.IsValid(_time.GetUtcNow()).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_BeforeExpiration_ReturnsTrue()
    {
        var apiKey = Create(expiresAt: _time.GetUtcNow().AddDays(30)).Value;

        _time.Advance(TimeSpan.FromDays(29));

        apiKey.IsValid(_time.GetUtcNow()).ShouldBeTrue();
    }

    [Fact]
    public void Revoke_ValidKey_MakesItInvalid()
    {
        var apiKey = Create().Value;

        apiKey.Revoke(_time.GetUtcNow());

        apiKey.IsValid(_time.GetUtcNow()).ShouldBeFalse();
        apiKey.RevokedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void Revoke_AlreadyRevoked_KeepsFirstRevocationTime()
    {
        var apiKey = Create().Value;
        var firstRevocation = _time.GetUtcNow();
        apiKey.Revoke(firstRevocation);

        _time.Advance(TimeSpan.FromHours(1));
        apiKey.Revoke(_time.GetUtcNow());

        apiKey.RevokedAt.ShouldBe(firstRevocation);
    }

    [Fact]
    public void RegisterUse_FirstUse_RecordsTime()
    {
        var apiKey = Create().Value;

        apiKey.RegisterUse(_time.GetUtcNow());

        apiKey.LastUsedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void RegisterUse_WithinWriteInterval_KeepsPreviousTime()
    {
        var apiKey = Create().Value;
        var firstUse = _time.GetUtcNow();
        apiKey.RegisterUse(firstUse);

        _time.Advance(ApiKey.UsageWriteInterval);
        apiKey.RegisterUse(_time.GetUtcNow());

        apiKey.LastUsedAt.ShouldBe(firstUse);
    }

    [Fact]
    public void RegisterUse_AfterWriteInterval_RecordsNewTime()
    {
        var apiKey = Create().Value;
        apiKey.RegisterUse(_time.GetUtcNow());

        _time.Advance(ApiKey.UsageWriteInterval + TimeSpan.FromSeconds(1));
        apiKey.RegisterUse(_time.GetUtcNow());

        apiKey.LastUsedAt.ShouldBe(_time.GetUtcNow());
    }

    private Result<ApiKey> Create(
        string? name = "Agent",
        UserRole role = UserRole.Member,
        DateTimeOffset? expiresAt = null) =>
        ApiKey.Create(name, Prefix, KeyHash, role, _adminId, expiresAt, _time.GetUtcNow());
}
