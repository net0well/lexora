namespace Lexora.Application.Abstractions.Ai;

public interface IChatModel
{
    Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken);

    IAsyncEnumerable<ChatStreamUpdate> StreamAsync(ChatRequest request, CancellationToken cancellationToken);
}
