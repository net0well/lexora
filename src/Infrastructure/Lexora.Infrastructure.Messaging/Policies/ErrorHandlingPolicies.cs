using JasperFx.Core;
using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Common.Validation;
using Wolverine;
using Wolverine.ErrorHandling;

namespace Lexora.Infrastructure.Messaging.Policies;

internal static class ErrorHandlingPolicies
{
    public static void Apply(WolverineOptions options)
    {
        options.Policies.OnException<InputValidationException>().Discard();

        options.Policies.OnException<AiProviderUnavailableException>()
            .RetryWithCooldown(2.Seconds(), 10.Seconds(), 30.Seconds())
            .Then.ScheduleRetry(2.Minutes(), 10.Minutes())
            .Then.MoveToErrorQueue();

        options.Policies.OnException<HttpRequestException>()
            .RetryWithCooldown(1.Seconds(), 5.Seconds(), 15.Seconds())
            .Then.MoveToErrorQueue();

        options.Policies.OnException<TimeoutException>()
            .RetryWithCooldown(1.Seconds(), 5.Seconds(), 15.Seconds())
            .Then.MoveToErrorQueue();
    }
}
