using Lexora.Domain.Users;

namespace Lexora.Application.Users.CreateUser;

public sealed record CreateUserCommand(string Email, string Name, UserRole Role, string Password);
