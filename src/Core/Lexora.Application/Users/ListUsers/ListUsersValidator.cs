using FluentValidation;
using Lexora.Application.Common.Validation;

namespace Lexora.Application.Users.ListUsers;

public sealed class ListUsersValidator : AbstractValidator<ListUsersQuery>
{
    public const int MaxSearchLength = 100;

    public ListUsersValidator()
    {
        this.ValidPage(query => query.Page, query => query.PageSize);

        RuleFor(query => query.Search).MaximumLength(MaxSearchLength).WithErrorCode("user.search.tooLong");
        RuleFor(query => query.Role).IsInEnum().WithErrorCode(UserNotifications.InvalidRoleKey);
        RuleFor(query => query.Status).IsInEnum().WithErrorCode("user.status.invalid");
    }
}
