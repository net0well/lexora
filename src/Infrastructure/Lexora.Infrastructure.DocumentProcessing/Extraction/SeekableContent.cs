namespace Lexora.Infrastructure.DocumentProcessing.Extraction;

public sealed class SeekableContent : IAsyncDisposable
{
    private const int BufferSize = 81920;

    private readonly FileStream? _buffer;

    private SeekableContent(Stream stream, FileStream? buffer)
    {
        Stream = stream;
        _buffer = buffer;
    }

    public Stream Stream { get; }

    public static async Task<SeekableContent> FromAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
            return new SeekableContent(content, buffer: null);
        }

        var buffer = new FileStream(
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.DeleteOnClose);

        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        return new SeekableContent(buffer, buffer);
    }

    public async ValueTask DisposeAsync()
    {
        if (_buffer is not null)
        {
            await _buffer.DisposeAsync();
        }
    }
}
