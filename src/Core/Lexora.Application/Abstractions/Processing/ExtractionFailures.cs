using Lexora.Shared.Results;

namespace Lexora.Application.Abstractions.Processing;

public static class ExtractionFailures
{
    public static Notification Encrypted { get; } =
        Notification.BusinessRule("document.extraction.encrypted", "Encrypted PDF files are not supported.");

    public static Notification NoText { get; } =
        Notification.BusinessRule("document.extraction.noText", "No extractable text found. Scanned documents (OCR) are not supported.");

    public static Notification PageLimit { get; } =
        Notification.BusinessRule("document.extraction.pageLimit", "Document exceeds the page limit.");

    public static Notification CellLimit { get; } =
        Notification.BusinessRule("document.extraction.cellLimit", "Spreadsheet exceeds the cell limit.");

    public static Notification Unreadable { get; } =
        Notification.BusinessRule("document.extraction.unreadable", "The file could not be read.");
}
