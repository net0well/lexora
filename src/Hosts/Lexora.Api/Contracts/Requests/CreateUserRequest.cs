using Lexora.Domain.Users;

namespace Lexora.Api.Contracts.Requests;

public sealed record CreateUserRequest(string Email, string Name, UserRole Role, string Password);
