using Lexora.Application;
using Microsoft.Extensions.Configuration;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Lexora.Infrastructure.Messaging;

public static class DependencyInjection
{
    public static WolverineOptions AddMessagingAdapter(
        this WolverineOptions options,
        IConfiguration configuration,
        MessagingHostRole role)
    {
        var rabbitMq = configuration.GetConnectionString(MessagingOptions.RabbitMqConnectionStringName);

        if (string.IsNullOrWhiteSpace(rabbitMq))
        {
            throw new InvalidOperationException($"Connection string '{MessagingOptions.RabbitMqConnectionStringName}' not found.");
        }

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

        options.UseRabbitMq(new Uri(rabbitMq)).AutoProvision();

        return options;
    }
}
