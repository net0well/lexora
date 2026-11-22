using Lexora.Shared.Domain;
using Lexora.Shared.Guards;

namespace Lexora.Domain.Conversations;

public sealed class ConversationMessage : Entity
{
    private readonly List<Citation> _citations = [];

    private ConversationMessage()
    {
    }

    private ConversationMessage(
        MessageRole role,
        string content,
        IEnumerable<Citation> citations,
        TokenUsage usage,
        string? provider,
        string? model,
        long? latencyMs,
        DateTimeOffset now)
        : base(Guid.CreateVersion7())
    {
        Role = role;
        Content = content;
        _citations.AddRange(citations);
        Usage = usage;
        Provider = provider;
        Model = model;
        LatencyMs = latencyMs;
        CreatedAt = now;
    }

    public MessageRole Role { get; private set; }

    public string Content { get; private set; } = null!;

    public IReadOnlyList<Citation> Citations => _citations.AsReadOnly();

    public TokenUsage Usage { get; private set; } = null!;

    public string? Provider { get; private set; }

    public string? Model { get; private set; }

    public long? LatencyMs { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal static ConversationMessage FromUser(string question, DateTimeOffset now) =>
        new(MessageRole.User, question, [], TokenUsage.None, null, null, null, now);

    internal static ConversationMessage FromAssistant(
        string answer,
        IEnumerable<Citation> citations,
        TokenUsage usage,
        string provider,
        string model,
        long latencyMs,
        DateTimeOffset now) =>
        new(
            MessageRole.Assistant,
            Guard.NotNullOrWhiteSpace(answer),
            Guard.NotNull(citations),
            Guard.NotNull(usage),
            Guard.NotNullOrWhiteSpace(provider),
            Guard.NotNullOrWhiteSpace(model),
            Guard.NotNegative(latencyMs),
            now);
}
