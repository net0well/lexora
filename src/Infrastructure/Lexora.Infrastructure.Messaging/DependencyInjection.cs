using Microsoft.Extensions.Configuration;
using Wolverine;

namespace Lexora.Infrastructure.Messaging;

public static class DependencyInjection
{
    public static WolverineOptions AddMessagingAdapter(
        this WolverineOptions options,
        IConfiguration configuration,
        MessagingHostRole role) => options;
}
