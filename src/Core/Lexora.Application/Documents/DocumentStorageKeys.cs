using Lexora.Domain.Documents;

namespace Lexora.Application.Documents;

public static class DocumentStorageKeys
{
    public static string PrefixOf(Guid knowledgeBaseId, Guid documentId) => $"knowledge-bases/{knowledgeBaseId}/documents/{documentId}/";

    public static string OriginalOf(Guid knowledgeBaseId, Guid documentId, DocumentFormat format) =>
        $"{PrefixOf(knowledgeBaseId, documentId)}original{DocumentFormats.ExtensionOf(format)}";

    public static string ExtractedOf(Guid knowledgeBaseId, Guid documentId, int processingVersion) =>
        $"{PrefixOf(knowledgeBaseId, documentId)}v{processingVersion}/extracted.json";
}
