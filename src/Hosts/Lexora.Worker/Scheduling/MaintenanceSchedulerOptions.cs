namespace Lexora.Worker.Scheduling;

public sealed class MaintenanceSchedulerOptions
{
    public const string SectionName = "Maintenance";

    public TimeSpan StalledDocumentsInterval { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan OrphanFilesInterval { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan ExpiredRefreshTokensInterval { get; init; } = TimeSpan.FromHours(24);
}
