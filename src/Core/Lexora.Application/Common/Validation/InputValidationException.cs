using Lexora.Shared.Results;

namespace Lexora.Application.Common.Validation;

public sealed class InputValidationException(IReadOnlyList<Notification> notifications)
    : Exception("Input validation failed.")
{
    public IReadOnlyList<Notification> Notifications { get; } = notifications;
}
