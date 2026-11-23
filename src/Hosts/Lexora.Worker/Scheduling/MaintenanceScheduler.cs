using Lexora.Application.Ingestion.Maintenance;
using Microsoft.Extensions.Options;
using Wolverine;

namespace Lexora.Worker.Scheduling;

public sealed partial class MaintenanceScheduler(
    IServiceScopeFactory scopeFactory,
    IOptions<MaintenanceSchedulerOptions> options,
    TimeProvider timeProvider,
    ILogger<MaintenanceScheduler> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        return Task.WhenAll(
            RunAsync<SweepStalledDocuments>(settings.StalledDocumentsInterval, stoppingToken),
            RunAsync<SweepOrphanFiles>(settings.OrphanFilesInterval, stoppingToken),
            RunAsync<SweepExpiredRefreshTokens>(settings.ExpiredRefreshTokensInterval, stoppingToken));
    }

    private async Task RunAsync<TMessage>(TimeSpan interval, CancellationToken stoppingToken)
        where TMessage : new()
    {
        using var timer = new PeriodicTimer(interval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PublishAsync(new TMessage());
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping the host cancels the wait for the next tick; that is the normal way out of the loop.
        }
    }

    private async Task PublishAsync<TMessage>(TMessage message)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(message);
        }
        catch (Exception exception)
        {
            // A failed publish only loses this tick; the next one schedules the sweep again.
            LogScheduleFailed(exception, typeof(TMessage).Name);
        }
    }

    [LoggerMessage(EventId = 4001, Level = LogLevel.Error, Message = "Failed to schedule maintenance message {MessageType}")]
    private partial void LogScheduleFailed(Exception exception, string messageType);
}
