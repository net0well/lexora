using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Lexora.Infrastructure.Storage.Local;

public sealed class LocalDownloadTickets(IDataProtectionProvider dataProtection)
{
    private const string Purpose = "Lexora.Storage.LocalDownload";

    private readonly ITimeLimitedDataProtector _protector = dataProtection.CreateProtector(Purpose).ToTimeLimitedDataProtector();

    public string Issue(LocalDownloadTicket ticket, DateTimeOffset expiresAt) =>
        _protector.Protect(JsonSerializer.Serialize(ticket), expiresAt);

    public bool TryRead(string value, [NotNullWhen(true)] out LocalDownloadTicket? ticket)
    {
        try
        {
            ticket = JsonSerializer.Deserialize<LocalDownloadTicket>(_protector.Unprotect(value));
            return ticket is not null;
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or JsonException)
        {
            ticket = null;
            return false;
        }
    }
}
