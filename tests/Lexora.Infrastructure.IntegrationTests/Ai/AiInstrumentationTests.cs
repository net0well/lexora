using System.Collections.Concurrent;
using System.Diagnostics;
using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Adapters;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.Fake;
using Lexora.Infrastructure.AI.Registry;
using Lexora.Infrastructure.AI.Resilience;
using Lexora.Infrastructure.AI.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ChatTurn = Lexora.Application.Abstractions.Ai.ChatTurn;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class AiInstrumentationTests : IDisposable
{
    private const string Question = "What is the salary of the director?";

    private readonly string _model = $"telemetry-{Guid.CreateVersion7():N}";
    private readonly ConcurrentQueue<Activity> _activities = new();
    private readonly ActivityListener _listener;

    public AiInstrumentationTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AiInstrumentation.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _activities.Enqueue,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CompleteAsync_ChatCall_RecordsModelAndTokensButNotTheConversation()
    {
        await AdapterWith(logPrompts: false, new FakeLoggerProvider()).CompleteAsync(Request(), CancellationToken);

        var activity = _activities.Where(activity => Equals(activity.GetTagItem("gen_ai.request.model"), _model)).ShouldHaveSingleItem();
        activity.GetTagItem("gen_ai.usage.input_tokens").ShouldNotBeNull();
        activity.GetTagItem("gen_ai.usage.output_tokens").ShouldNotBeNull();
        activity.TagObjects.ShouldAllBe(tag => !(tag.Value ?? string.Empty).ToString()!.Contains("salary", StringComparison.Ordinal));
        activity.Events.ShouldAllBe(activityEvent => !activityEvent.Tags.Any(tag => (tag.Value ?? string.Empty).ToString()!.Contains("salary", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompleteAsync_PromptLogging_OnlyWritesTheDetailedLogWhenAskedFor(bool logPrompts)
    {
        using var logs = new FakeLoggerProvider();

        await AdapterWith(logPrompts, logs).CompleteAsync(Request(), CancellationToken);

        logs.Collector.GetSnapshot().Any(record => record.Message.Contains("salary", StringComparison.Ordinal)).ShouldBe(logPrompts);
    }

    public void Dispose() => _listener.Dispose();

    private ChatModelAdapter AdapterWith(bool logPrompts, FakeLoggerProvider logs)
    {
        var options = new AiOptions
        {
            LogPrompts = logPrompts,
            Chat = new AiChatSettings { DefaultProfile = "fake", Profiles = { ["fake"] = new AiProfileSettings { Provider = AiProviderKind.Fake, Model = _model } } },
        };
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(logs));
        var registry = new ChatClientRegistry([new FakeChatClientProvider()], Microsoft.Extensions.Options.Options.Create(options), loggerFactory);

        return new ChatModelAdapter(registry, new AiResiliencePipelineProvider(TimeProvider.System), TimeProvider.System);
    }

    private static ChatRequest Request() => new(null, "Answer from the excerpts.", [new ChatTurn(ChatTurnRole.User, Question)]);
}
