using Lexora.Application.Common.Options;
using Lexora.Application.Conversations.Prompts;
using Lexora.Application.Retrieval;
using Lexora.Application.Retrieval.Services;
using Lexora.Domain.Conversations;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Conversations.Services;

public sealed class AnswerContextBuilder(
    IRetrievalService retrieval,
    IRagPromptBuilder prompts,
    IFollowUpQuestionRewriter rewriter,
    IOptions<RagOptions> options) : IAnswerContextBuilder
{
    public async Task<AnswerContext> BuildAsync(
        Guid knowledgeBaseId,
        string question,
        IReadOnlyList<ConversationMessage> history,
        string? profile,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var searchQuery = settings.RewriteFollowUpQuestions
            ? await rewriter.RewriteAsync(question, history, profile, cancellationToken)
            : question;
        var hits = await retrieval.RetrieveAsync(knowledgeBaseId, searchQuery, SearchMode.Hybrid, settings.TopK, cancellationToken);

        return new AnswerContext(question, hits.Count == 0 ? null : prompts.Build(question, hits, history));
    }
}
