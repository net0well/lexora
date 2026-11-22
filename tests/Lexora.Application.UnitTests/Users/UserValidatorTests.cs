using Lexora.Application.Users;
using Lexora.Application.Users.ChangeUserRole;
using Lexora.Application.Users.CreateUser;
using Lexora.Application.Users.ResetUserPassword;
using Lexora.Domain.Users;

namespace Lexora.Application.UnitTests.Users;

public sealed class UserValidatorTests
{
    [Fact]
    public void CreateUser_ValidInput_HasNoErrors() =>
        new CreateUserValidator().Validate(new CreateUserCommand("ana@lexora.local", "Ana", UserRole.Viewer, "Str0ngPassword")).IsValid.ShouldBeTrue();

    [Fact]
    public void CreateUser_EmptyInput_ReportsEveryFieldByKey()
    {
        var result = new CreateUserValidator().Validate(new CreateUserCommand("", "", (UserRole)99, ""));

        result.Errors.Select(error => error.ErrorCode).ShouldBe(
            ["user.email.invalid", "user.name.required", UserNotifications.InvalidRoleKey, "auth.password.weak"],
            ignoreOrder: true);
    }

    [Fact]
    public void ChangeUserRole_UndefinedRole_IsInvalid() =>
        new ChangeUserRoleValidator().Validate(new ChangeUserRoleCommand(Guid.CreateVersion7(), (UserRole)0))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(UserNotifications.InvalidRoleKey);

    [Fact]
    public void ResetUserPassword_WeakPassword_IsInvalid() =>
        new ResetUserPasswordValidator().Validate(new ResetUserPasswordCommand(Guid.CreateVersion7(), "short"))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("auth.password.weak");
}
