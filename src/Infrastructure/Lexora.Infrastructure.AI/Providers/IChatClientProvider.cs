using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers;

public interface IChatClientProvider
{
    AiProviderKind Kind { get; }

    IChatClient Create(string profileName, AiProfileSettings settings);
}
