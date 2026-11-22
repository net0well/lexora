namespace Lexora.Application.Users.ResetUserPassword;

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword);
