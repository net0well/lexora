using Lexora.Shared.Domain;
using Lexora.Shared.Guards;
using Lexora.Shared.Results;

namespace Lexora.Domain.Conversations;

public sealed class Conversation : AggregateRoot
{
    public const int MaxMessages = 200;
    public const int MaxQuestionLength = 4000;
    public const int MaxTitleLength = 80;

    private const string TitleEllipsis = "...";

    private static readonly Notification _titleRequired = Notification.Validation("conversation.title.required", "The title is required.");

    private static readonly Notification _titleTooLong =
        Notification.Validation("conversation.title.tooLong", $"The title cannot be longer than {MaxTitleLength} characters.");

    private static readonly Notification _messageRequired = Notification.Validation("conversation.message.required", "The question is required.");

    private static readonly Notification _messageTooLong =
        Notification.Validation("conversation.message.tooLong", $"The question cannot be longer than {MaxQuestionLength} characters.");

    private static readonly Notification _messageLimit =
        Notification.BusinessRule("conversation.limit.messages", $"A conversation can have at most {MaxMessages} messages. Start a new one.");

    private readonly List<ConversationMessage> _messages = [];

    private Conversation()
    {
    }

    private Conversation(Guid knowledgeBaseId, Guid userId, string title, DateTimeOffset now)
        : base(Guid.CreateVersion7())
    {
        KnowledgeBaseId = knowledgeBaseId;
        UserId = userId;
        Title = title;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid KnowledgeBaseId { get; private set; }

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = null!;

    public IReadOnlyList<ConversationMessage> Messages => _messages.AsReadOnly();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Conversation> Start(Guid knowledgeBaseId, Guid userId, string? title, DateTimeOffset now)
    {
        Guard.NotEmpty(knowledgeBaseId);
        Guard.NotEmpty(userId);

        var trimmed = title?.Trim() ?? string.Empty;

        return trimmed.Length <= MaxTitleLength
            ? new Conversation(knowledgeBaseId, userId, trimmed, now)
            : Result.Failure<Conversation>(_titleTooLong);
    }

    public Result Rename(string? title, DateTimeOffset now)
    {
        var trimmed = title?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return Result.Failure(_titleRequired);
        }

        if (trimmed.Length > MaxTitleLength)
        {
            return Result.Failure(_titleTooLong);
        }

        Title = trimmed;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result<ConversationMessage> AddUserMessage(string? question, DateTimeOffset now)
    {
        var trimmed = question?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return Result.Failure<ConversationMessage>(_messageRequired);
        }

        if (trimmed.Length > MaxQuestionLength)
        {
            return Result.Failure<ConversationMessage>(_messageTooLong);
        }

        if (_messages.Count >= MaxMessages)
        {
            return Result.Failure<ConversationMessage>(_messageLimit);
        }

        if (Title.Length == 0)
        {
            Title = TitleFrom(trimmed);
        }

        return Append(ConversationMessage.FromUser(trimmed, now), now);
    }

    public Result<ConversationMessage> AddAssistantMessage(
        string answer,
        IReadOnlyList<Citation> citations,
        TokenUsage usage,
        string provider,
        string model,
        long latencyMs,
        DateTimeOffset now)
    {
        var message = ConversationMessage.FromAssistant(answer, citations, usage, provider, model, latencyMs, now);

        return _messages.Count >= MaxMessages
            ? Result.Failure<ConversationMessage>(_messageLimit)
            : Append(message, now);
    }

    public bool IsOwnedBy(Guid userId) => UserId == userId;

    public IReadOnlyList<ConversationMessage> RecentHistory(int maxMessages) => [.. _messages.TakeLast(Guard.NotNegative(maxMessages))];

    private ConversationMessage Append(ConversationMessage message, DateTimeOffset now)
    {
        _messages.Add(message);
        UpdatedAt = now;

        return message;
    }

    private static string TitleFrom(string question)
    {
        var singleLine = string.Join(' ', question.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));

        return singleLine.Length <= MaxTitleLength
            ? singleLine
            : $"{singleLine[..(MaxTitleLength - TitleEllipsis.Length)].TrimEnd()}{TitleEllipsis}";
    }
}
