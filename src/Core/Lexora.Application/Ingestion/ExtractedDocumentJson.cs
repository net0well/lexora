using System.Text.Json;
using Lexora.Application.Abstractions.Processing;

namespace Lexora.Application.Ingestion;

public static class ExtractedDocumentJson
{
    public const string ContentType = "application/json";

    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public static async Task<MemoryStream> SerializeAsync(ExtractedDocument document, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await JsonSerializer.SerializeAsync(buffer, document, _options, cancellationToken);
        buffer.Position = 0;

        return buffer;
    }

    public static async Task<ExtractedDocument> DeserializeAsync(Stream content, CancellationToken cancellationToken) =>
        await JsonSerializer.DeserializeAsync<ExtractedDocument>(content, _options, cancellationToken)
        ?? throw new InvalidDataException("The extracted text file is empty.");
}
