namespace Lexora.Domain.Documents;

public enum DocumentStatus
{
    Uploaded = 1,
    Extracted = 2,
    Chunked = 3,
    Indexed = 4,
    Failed = 5,
    Deleted = 6
}
