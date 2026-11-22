using Lexora.Shared.Domain;
using Lexora.Shared.Guards;

namespace Lexora.Domain.Documents;

public sealed class UploadLimits : ValueObject
{
    private UploadLimits(long maxFileSizeBytes, long maxTotalStorageBytes)
    {
        MaxFileSizeBytes = maxFileSizeBytes;
        MaxTotalStorageBytes = maxTotalStorageBytes;
    }

    public long MaxFileSizeBytes { get; }

    public long MaxTotalStorageBytes { get; }

    public static UploadLimits Create(long maxFileSizeBytes, long maxTotalStorageBytes)
    {
        Guard.Positive(maxFileSizeBytes);
        Guard.Positive(maxTotalStorageBytes);

        return maxFileSizeBytes <= maxTotalStorageBytes
            ? new UploadLimits(maxFileSizeBytes, maxTotalStorageBytes)
            : throw new ArgumentOutOfRangeException(
                nameof(maxFileSizeBytes),
                maxFileSizeBytes,
                "The file size limit cannot exceed the total storage limit.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxFileSizeBytes;
        yield return MaxTotalStorageBytes;
    }
}
