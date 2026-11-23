using Lexora.Application.Ingestion.Maintenance;
using Lexora.TestKit;
using Lexora.Worker.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Wolverine;

namespace Lexora.Infrastructure.IntegrationTests.Worker;

public sealed class MaintenanceSchedulerTests : IAsyncDisposable
{
    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly FakeLogger<MaintenanceScheduler> _logger = new();
    private readonly ServiceProvider _services;
    private readonly MaintenanceScheduler _scheduler;
    private int _failuresLeft;

    public MaintenanceSchedulerTests()
    {
        _services = new ServiceCollection().AddScoped(_ => NextBus()).BuildServiceProvider();
        _scheduler = new MaintenanceScheduler(
            _services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new MaintenanceSchedulerOptions()),
            _time,
            _logger);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExecuteAsync_FiveMinutesPass_SchedulesOnlyTheStalledDocumentsSweep()
    {
        await _scheduler.StartAsync(CancellationToken);

        await AdvanceUntilAsync(TimeSpan.FromMinutes(1), () => Published<SweepStalledDocuments>() > 0);

        Published<SweepOrphanFiles>().ShouldBe(0);
        Published<SweepExpiredRefreshTokens>().ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_ADayPasses_SchedulesTheDailySweeps()
    {
        await _scheduler.StartAsync(CancellationToken);

        await AdvanceUntilAsync(
            TimeSpan.FromHours(1),
            () => Published<SweepOrphanFiles>() > 0 && Published<SweepExpiredRefreshTokens>() > 0);

        Published<SweepStalledDocuments>().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteAsync_PublishFails_LogsAndSchedulesAgainOnTheNextTick()
    {
        _failuresLeft = 1;

        await _scheduler.StartAsync(CancellationToken);

        await AdvanceUntilAsync(TimeSpan.FromMinutes(1), () => Published<SweepStalledDocuments>() > 0);

        var failure = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        failure.Level.ShouldBe(LogLevel.Error);
        failure.Message.ShouldContain(nameof(SweepStalledDocuments));
    }

    public async ValueTask DisposeAsync()
    {
        await _scheduler.StopAsync(CancellationToken.None);
        _scheduler.Dispose();
        await _services.DisposeAsync();
    }

    private IMessageBus NextBus() =>
        Interlocked.Decrement(ref _failuresLeft) >= 0 ? throw new InvalidOperationException("Broker unavailable.") : _bus;

    private int Published<TMessage>() =>
        _bus.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IMessageBus.PublishAsync) && call.GetArguments()[0] is TMessage);

    // The timers are created on a background thread after StartAsync, so time moves in small steps
    // until the expected messages show up instead of jumping once and racing the timer creation.
    private async Task AdvanceUntilAsync(TimeSpan step, Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            _time.Advance(step);
            await Task.Delay(TimeSpan.FromMilliseconds(10), CancellationToken);
        }

        condition().ShouldBeTrue();
    }
}
