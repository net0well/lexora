using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Adapters;

public sealed record ChatStreamStart(IAsyncEnumerator<ChatResponseUpdate> Updates, bool HasCurrent);
