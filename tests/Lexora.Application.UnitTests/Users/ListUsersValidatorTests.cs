using Lexora.Application.Common.Validation;
using Lexora.Application.Users.ListUsers;
using Lexora.Domain.Users;

namespace Lexora.Application.UnitTests.Users;

public sealed class ListUsersValidatorTests
{
    private readonly ListUsersValidator _validator = new();

    [Fact]
    public void Validate_Defaults_AreValid() => _validator.Validate(new ListUsersQuery(null, null, null)).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(0, 20, PagingRuleExtensions.InvalidPageKey)]
    [InlineData(1, 0, PagingRuleExtensions.InvalidPageSizeKey)]
    [InlineData(1, 101, PagingRuleExtensions.InvalidPageSizeKey)]
    public void Validate_PagingOutOfRange_ReportsTheKey(int page, int pageSize, string expectedKey) =>
        _validator.Validate(new ListUsersQuery(null, null, null, page, pageSize)).Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(expectedKey);

    [Fact]
    public void Validate_UndefinedFilters_AreInvalid()
    {
        var result = _validator.Validate(new ListUsersQuery(new string('a', ListUsersValidator.MaxSearchLength + 1), (UserRole)42, (UserStatus)42));

        result.Errors.Select(error => error.ErrorCode).ShouldBe(["user.search.tooLong", "user.role.invalid", "user.status.invalid"], ignoreOrder: true);
    }
}
