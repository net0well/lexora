using System.IO.Compression;
using System.Text;
using Lexora.Domain.Documents;

namespace Lexora.Infrastructure.DocumentProcessing.Detection;

public static class FileSignatureInspector
{
    private const int BufferSize = 81920;
    private const string ContentTypesEntry = "[Content_Types].xml";

    private static readonly byte[] _pdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] _zipSignature = [0x50, 0x4B, 0x03, 0x04];

    public static async Task<DocumentFormat?> DetectAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanSeek)
        {
            throw new ArgumentException("The content must be seekable.", nameof(content));
        }

        try
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => await StartsWithAsync(content, _pdfSignature, cancellationToken) ? DocumentFormat.Pdf : null,
                ".docx" => await IsOpenXmlAsync(content, "word/document.xml", cancellationToken) ? DocumentFormat.Docx : null,
                ".xlsx" => await IsOpenXmlAsync(content, "xl/workbook.xml", cancellationToken) ? DocumentFormat.Xlsx : null,
                ".txt" => await IsUtf8TextAsync(content, cancellationToken) ? DocumentFormat.Txt : null,
                ".md" or ".markdown" => await IsUtf8TextAsync(content, cancellationToken) ? DocumentFormat.Markdown : null,
                ".csv" => await IsUtf8TextAsync(content, cancellationToken) ? DocumentFormat.Csv : null,
                _ => null,
            };
        }
        finally
        {
            content.Position = 0;
        }
    }

    private static async Task<bool> StartsWithAsync(Stream content, byte[] signature, CancellationToken cancellationToken)
    {
        content.Position = 0;
        var header = new byte[signature.Length];
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);

        return read == signature.Length && header.AsSpan().SequenceEqual(signature);
    }

    private static async Task<bool> IsOpenXmlAsync(Stream content, string mainPart, CancellationToken cancellationToken)
    {
        if (!await StartsWithAsync(content, _zipSignature, cancellationToken))
        {
            return false;
        }

        content.Position = 0;

        try
        {
            await using var archive = await ZipArchive.CreateAsync(content, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: null, cancellationToken);

            return archive.GetEntry(ContentTypesEntry) is not null && archive.GetEntry(mainPart) is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static async Task<bool> IsUtf8TextAsync(Stream content, CancellationToken cancellationToken)
    {
        content.Position = 0;
        var decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetDecoder();
        var bytes = new byte[BufferSize];
        var chars = new char[BufferSize + 1];

        try
        {
            int read;

            while ((read = await content.ReadAsync(bytes, cancellationToken)) > 0)
            {
                if (bytes.AsSpan(0, read).Contains((byte)0))
                {
                    return false;
                }

                decoder.GetChars(bytes, 0, read, chars, 0, flush: false);
            }

            decoder.GetChars([], 0, 0, chars, 0, flush: true);

            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
