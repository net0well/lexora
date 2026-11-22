using Lexora.Api.Contracts.Requests;
using Lexora.Api.Mappers;
using Lexora.Application.Auth.ChangePassword;
using Lexora.Application.Auth.Login;
using Lexora.Application.Users.ChangeUserRole;
using Lexora.Application.Users.CreateUser;
using Lexora.Application.Users.ResetUserPassword;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Mappers;

public sealed class RequestMapperTests
{
    private static readonly Guid _userId = Guid.CreateVersion7();

    [Fact]
    public void ToCommand_Login_CopiesCredentials() =>
        new LoginRequest("ana@lexora.local", "secret").ToCommand().ShouldBe(new LoginCommand("ana@lexora.local", "secret"));

    [Fact]
    public void ToCommand_ChangePassword_CopiesBothPasswords() =>
        new ChangePasswordRequest("old", "new").ToCommand().ShouldBe(new ChangePasswordCommand("old", "new"));

    [Fact]
    public void ToCommand_CreateUser_CopiesEveryField() =>
        new CreateUserRequest("ana@lexora.local", "Ana", UserRole.Viewer, "pw").ToCommand()
            .ShouldBe(new CreateUserCommand("ana@lexora.local", "Ana", UserRole.Viewer, "pw"));

    [Fact]
    public void ToCommand_ChangeUserRole_TakesTheIdFromTheRoute() =>
        new ChangeUserRoleRequest(UserRole.Member).ToCommand(_userId).ShouldBe(new ChangeUserRoleCommand(_userId, UserRole.Member));

    [Fact]
    public void ToCommand_ResetUserPassword_TakesTheIdFromTheRoute() =>
        new ResetUserPasswordRequest("pw").ToCommand(_userId).ShouldBe(new ResetUserPasswordCommand(_userId, "pw"));
}
