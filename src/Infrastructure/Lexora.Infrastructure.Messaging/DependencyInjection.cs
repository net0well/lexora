using Lexora.Application;
using Microsoft.Extensions.Configuration;
using Wolverine;

namespace Lexora.Infrastructure.Messaging;

public static class DependencyInjection
{
    public static WolverineOptions AddMessagingAdapter(
        this WolverineOptions options,
        IConfiguration configuration,
        MessagingHostRole role)
    {
        options.ServiceName = role switch
        {
            MessagingHostRole.Api => "lexora-api",
            MessagingHostRole.Worker => "lexora-worker",
            MessagingHostRole.Mcp => "lexora-mcp",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown messaging host role."),
        };

        options.Discovery.IncludeAssembly(typeof(ApplicationAssembly).Assembly);

        options.Policies.AutoApplyTransactions();
        options.Policies.UseDurableLocalQueues();
        options.Policies.UseDurableInboxOnAllListeners();
        options.Policies.UseDurableOutboxOnAllSendingEndpoints();

        options.Durability.Mode = DurabilityMode.Balanced;
        options.Durability.MessageIdentity = MessageIdentity.IdAndDestination;

        return options;
    }
}
