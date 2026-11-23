using System.Net;
using Lexora.Infrastructure.AI.Resilience;
using Polly.CircuitBreaker;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class AiFailureClassifierTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData((HttpStatusCode)529, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public void IsTransient_HttpFailure_DependsOnTheStatus(HttpStatusCode? status, bool expected) =>
        AiFailureClassifier.IsTransient(new HttpRequestException("failure", null, status), CancellationToken.None).ShouldBe(expected);

    [Fact]
    public async Task IsTransient_Timeout_IsTransientUnlessTheCallerCancelled()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        AiFailureClassifier.IsTransient(new TaskCanceledException(), CancellationToken.None).ShouldBeTrue();
        AiFailureClassifier.IsTransient(new TaskCanceledException(), cancelled.Token).ShouldBeFalse();
    }

    [Fact]
    public void IsUnavailable_OpenCircuit_IsUnavailableButNotRetried()
    {
        var exception = new BrokenCircuitException();

        AiFailureClassifier.IsUnavailable(exception, CancellationToken.None).ShouldBeTrue();
        AiFailureClassifier.IsTransient(exception, CancellationToken.None).ShouldBeFalse();
    }

    [Fact]
    public void IsUnavailable_ProgrammingError_IsNot() =>
        AiFailureClassifier.IsUnavailable(new InvalidOperationException("bug"), CancellationToken.None).ShouldBeFalse();
}
