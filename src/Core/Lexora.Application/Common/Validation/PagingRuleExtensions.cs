using FluentValidation;
using Lexora.Application.Common.Paging;

namespace Lexora.Application.Common.Validation;

public static class PagingRuleExtensions
{
    public const string InvalidPageKey = "paging.page.invalid";
    public const string InvalidPageSizeKey = "paging.pageSize.invalid";

    public static void ValidPage<T>(this AbstractValidator<T> validator, Func<T, int> page, Func<T, int> pageSize)
    {
        validator.RuleFor(model => page(model))
            .GreaterThanOrEqualTo(1)
            .WithName("Page")
            .WithErrorCode(InvalidPageKey);

        validator.RuleFor(model => pageSize(model))
            .InclusiveBetween(1, PageRequest.MaxPageSize)
            .WithName("PageSize")
            .WithErrorCode(InvalidPageSizeKey);
    }
}
