using Lexora.Application.Auth;

namespace Lexora.Api.Contracts.Responses;

public sealed record SessionResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthenticatedUserResponse User);
