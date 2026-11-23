using FluentValidation;
using Lexora.Application.Common.Validation;

namespace Lexora.Application.Conversations.ListConversations;

public sealed class ListConversationsValidator : AbstractValidator<ListConversationsQuery>
{
    public ListConversationsValidator() => this.ValidPage(query => query.Page, query => query.PageSize);
}
