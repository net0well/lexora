using System.Net.Http.Headers;
using System.Text;

namespace Lexora.Infrastructure.Storage.S3;

public static class AttachmentDisposition
{
    public static string For(string fileName) => new ContentDispositionHeaderValue("attachment")
    {
        FileName = $"\"{AsciiFallback(fileName)}\"",
        FileNameStar = fileName,
    }.ToString();

    private static string AsciiFallback(string fileName)
    {
        var fallback = new StringBuilder(fileName.Length);

        foreach (var character in fileName)
        {
            fallback.Append(character is >= ' ' and <= '~' and not '"' and not '\\' ? character : '_');
        }

        return fallback.ToString();
    }
}
