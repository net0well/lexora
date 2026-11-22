using Lexora.Application;
using Lexora.Infrastructure.Messaging.Policies;
using Lexora.Infrastructure.Messaging.Routing;
using Lexora.Infrastructure.Messaging.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.FluentValidation;
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

        var applicationAssembly = typeof(ApplicationAssembly).Assembly;
        options.Discovery.IncludeAssembly(applicationAssembly);

        // The host is the Wolverine application assembly (it holds the pre-generated code), so its own
        // *Handler types, such as ASP.NET Core result handlers, must not be taken for message handlers.
        options.Discovery.CustomizeHandlerDiscovery(query =>
            query.Excludes.WithCondition("Outside the application layer", type => type.Assembly != applicationAssembly));

        options.UseFluentValidation();
        options.Services.AddSingleton(typeof(IFailureAction<>), typeof(NotificationFailureAction<>));

        options.Policies.AutoApplyTransactions();
        options.Policies.UseDurableLocalQueues();
        options.Policies.UseDurableInboxOnAllListeners();
        options.Policies.UseDurableOutboxOnAllSendingEndpoints();

        options.Durability.Mode = DurabilityMode.Balanced;
        options.Durability.MessageIdentity = MessageIdentity.IdAndDestination;

        options.UseRabbitMq(new Uri(rabbitMq)).AutoProvision();

        ErrorHandlingPolicies.Apply(options);

        if (role == MessagingHostRole.Api)
        {
            ApiRouting.Apply(options);
        }

        return options;
    }
}
