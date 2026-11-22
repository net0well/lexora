using Lexora.Domain.Documents;

namespace Lexora.Application.Documents;

public static class DocumentFormats
{
    public static string ExtensionOf(DocumentFormat format) => format switch
    {
        DocumentFormat.Pdf => ".pdf",
        DocumentFormat.Docx => ".docx",
        DocumentFormat.Xlsx => ".xlsx",
        DocumentFormat.Txt => ".txt",
        DocumentFormat.Markdown => ".md",
        DocumentFormat.Csv => ".csv",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown document format."),
    };

    public static string ContentTypeOf(DocumentFormat format) => format switch
    {
        DocumentFormat.Pdf => "application/pdf",
        DocumentFormat.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        DocumentFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        DocumentFormat.Txt => "text/plain; charset=utf-8",
        DocumentFormat.Markdown => "text/markdown; charset=utf-8",
        DocumentFormat.Csv => "text/csv; charset=utf-8",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown document format."),
    };
}
