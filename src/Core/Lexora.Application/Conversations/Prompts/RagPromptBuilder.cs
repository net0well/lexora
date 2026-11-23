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
    private static readonly PromptTemplate _system = PromptTemplate.Load("answer-system");
    private static readonly PromptTemplate _user = PromptTemplate.Load("answer-user");

    public static string SystemPrompt => _system.Text;

    public RagPrompt Build(string question, IReadOnlyList<SearchHit> hits, IReadOnlyList<ConversationMessage> history)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(hits);
        ArgumentNullException.ThrowIfNull(history);

        var excerpts = Excerpts(hits, options.Value.MaxContextTokens);

        List<ChatTurn> turns =
        [
            .. history.Select(message => new ChatTurn(message.Role == MessageRole.User ? ChatTurnRole.User : ChatTurnRole.Assistant, message.Content)),
            new ChatTurn(ChatTurnRole.User, Question(question.Trim(), excerpts)),
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
            var cost = tokens.Count(Document(excerpts.Count + 1, hit));

            if (used + cost <= budget)
            {
                excerpts.Add(new RagExcerpt(excerpts.Count + 1, hit));
                used += cost;
            }
        }

        return excerpts;
    }

    private static string Question(string question, List<RagExcerpt> excerpts) =>
        _user.Render(new Dictionary<string, string>
        {
            ["documents"] = string.Join('\n', excerpts.Select(excerpt => Document(excerpt.Number, excerpt.Hit))),
            ["question"] = PromptText.Neutralize(question),
            ["language"] = LanguageName(QuestionLanguageDetector.Detect(question)),
        });

    private static string Document(int number, SearchHit hit)
    {
        var document = new StringBuilder();
        document.Append(CultureInfo.InvariantCulture, $"<document index=\"{number}\">\n");
        document.Append("<source>").Append(PromptText.Neutralize(hit.DocumentName)).Append("</source>\n");

        if (hit.PageNumber is { } page)
        {
            document.Append(CultureInfo.InvariantCulture, $"<page>{page}</page>\n");
        }

        if (!string.IsNullOrWhiteSpace(hit.SheetName))
        {
            document.Append("<sheet>").Append(PromptText.Neutralize(hit.SheetName)).Append("</sheet>\n");
        }

        if (!string.IsNullOrWhiteSpace(hit.SectionTitle))
        {
            document.Append("<section>").Append(PromptText.Neutralize(hit.SectionTitle)).Append("</section>\n");
        }

        return document
            .Append("<document_content>\n")
            .Append(PromptText.Neutralize(hit.Content))
            .Append("\n</document_content>\n</document>")
            .ToString();
    }

    private static string LanguageName(QuestionLanguage language) => language switch
    {
        QuestionLanguage.Portuguese => "Portuguese",
        QuestionLanguage.Spanish => "Spanish",
        QuestionLanguage.English => "English",
        _ => "the language of the question",
    };
}
