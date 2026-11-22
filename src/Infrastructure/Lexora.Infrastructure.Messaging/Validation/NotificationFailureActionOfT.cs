using FluentValidation.Results;
using Lexora.Application.Common.Validation;
using Lexora.Shared.Results;
using Wolverine.FluentValidation;

namespace Lexora.Infrastructure.Messaging.Validation;

public sealed class NotificationFailureAction<T> : IFailureAction<T>
{
    public void Throw(T message, IReadOnlyList<ValidationFailure> failures) =>
        throw new InputValidationException(
        [
            .. failures.Select(failure => Notification.Validation(
                string.IsNullOrWhiteSpace(failure.ErrorCode) ? failure.PropertyName : failure.ErrorCode,
                failure.ErrorMessage)),
        ]);
}
