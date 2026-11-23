using System.Globalization;
using System.Text;
using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Common.Options;
using Lexora.Application.Retrieval;
using Lexora.Domain.Conversations;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Conversations.Prompts;

public sealed class RagPromptBuilder(ITokenCounter tokens, IOptions<RagOptions> options) : IRagPromptBuilder
{
    private static readonly Lazy<string> _systemPrompt = new(LoadSystemPrompt);

    public static string SystemPrompt => _systemPrompt.Value;

    public RagPrompt Build(string question, IReadOnlyList<SearchHit> hits, IReadOnlyList<ConversationMessage> history)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(hits);
        ArgumentNullException.ThrowIfNull(history);

        var excerpts = Excerpts(hits, options.Value.MaxContextTokens);

        List<ChatTurn> turns =
        [
            .. history.Select(message => new ChatTurn(message.Role == MessageRole.User ? ChatTurnRole.User : ChatTurnRole.Assistant, message.Content)),
            new ChatTurn(ChatTurnRole.User, Question(question, excerpts)),
        ];

        return new RagPrompt(SystemPrompt, turns, excerpts);
    }

    // Excerpts that would overflow the budget are skipped, so a long one does not keep shorter, relevant ones out.
    private List<RagExcerpt> Excerpts(IReadOnlyList<SearchHit> hits, int budget)
    {
        List<RagExcerpt> excerpts = [];
        var used = 0;

        foreach (var hit in hits)
        {
            var cost = tokens.Count(Block(excerpts.Count + 1, hit));

            if (used + cost <= budget)
            {
                excerpts.Add(new RagExcerpt(excerpts.Count + 1, hit));
                used += cost;
            }
        }

        return excerpts;
    }

    private static string Question(string question, List<RagExcerpt> excerpts)
    {
        var text = new StringBuilder("<context>\n");
        text.AppendJoin("\n\n", excerpts.Select(excerpt => Block(excerpt.Number, excerpt.Hit)));
        text.Append("\n</context>\n\nQuestion: ").Append(question.Trim());

        return text.ToString();
    }

    private static string Block(int number, SearchHit hit)
    {
        var header = new StringBuilder();
        header.Append(CultureInfo.InvariantCulture, $"[{number}] Document: {hit.DocumentName}");

        if (hit.PageNumber is { } page)
        {
            header.Append(CultureInfo.InvariantCulture, $" | Page: {page}");
        }

        if (!string.IsNullOrWhiteSpace(hit.SheetName))
        {
            header.Append(CultureInfo.InvariantCulture, $" | Sheet: {hit.SheetName}");
        }

        if (!string.IsNullOrWhiteSpace(hit.SectionTitle))
        {
            header.Append(CultureInfo.InvariantCulture, $" | Section: {hit.SectionTitle}");
        }

        return header.Append('\n').Append(hit.Content).ToString();
    }

    private static string LoadSystemPrompt()
    {
        using var stream = typeof(RagPromptBuilder).Assembly.GetManifestResourceStream(
            "Lexora.Application.Conversations.Prompts.Templates.rag-system-prompt.txt")
            ?? throw new InvalidOperationException("The RAG system prompt resource is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd().Trim();
    }
}
