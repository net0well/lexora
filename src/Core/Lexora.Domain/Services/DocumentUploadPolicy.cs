using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Shared.Guards;
using Lexora.Shared.Results;

namespace Lexora.Domain.Services;

public static class DocumentUploadPolicy
{
    public static Result Evaluate(
        KnowledgeBase knowledgeBase,
        DocumentFormat? format,
        long sizeBytes,
        UploadLimits limits,
        long currentStorageBytes)
    {
        Guard.NotNull(knowledgeBase);
        Guard.NotNull(limits);
        Guard.NotNegative(currentStorageBytes);

        List<Notification> notifications = [.. knowledgeBase.EnsureAcceptsUploads().Notifications];

        if (format is not { } knownFormat || !Enum.IsDefined(knownFormat))
        {
            notifications.Add(Notification.Validation("document.format.unsupported", "The file format is not supported."));
        }

        if (sizeBytes <= 0)
        {
            notifications.Add(Notification.Validation("document.size.empty", "The file is empty."));
        }
        else
        {
            if (sizeBytes > limits.MaxFileSizeBytes)
            {
                notifications.Add(Notification.Validation(
                    "document.size.exceeded",
                    $"The file exceeds the limit of {limits.MaxFileSizeBytes} bytes."));
            }

            if (sizeBytes > limits.MaxTotalStorageBytes - currentStorageBytes)
            {
                notifications.Add(Notification.BusinessRule("storage.limit.exceeded", "The storage limit would be exceeded."));
            }
        }

        return notifications.Count == 0 ? Result.Success() : Result.Failure(notifications);
    }
}
