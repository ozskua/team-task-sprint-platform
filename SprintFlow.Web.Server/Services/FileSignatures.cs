namespace SprintFlow.Web.Server.Services;

public static class FileSignatures
{
    public const int HeadLength = 12;

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Gif = [0x47, 0x49, 0x46, 0x38];
    private static readonly byte[] Riff = [0x52, 0x49, 0x46, 0x46];
    private static readonly byte[] Webp = [0x57, 0x45, 0x42, 0x50];
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46];
    private static readonly byte[] Zip = [0x50, 0x4B];

    public static bool Matches(ReadOnlySpan<byte> head, string contentType)
        => contentType switch
        {
            "image/png" => head.StartsWith(Png),
            "image/jpeg" => head.StartsWith(Jpeg),
            "image/gif" => head.StartsWith(Gif),
            "image/webp" => head.StartsWith(Riff) && head.Length >= 12 && head.Slice(8, 4).SequenceEqual(Webp),
            "application/pdf" => head.StartsWith(Pdf),
            "application/zip" => head.StartsWith(Zip),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => head.StartsWith(Zip),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => head.StartsWith(Zip),
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => head.StartsWith(Zip),
            _ => true
        };

    public static bool DeclaredTypeIsConsistent(string? declared, string expected)
    {
        if (string.IsNullOrWhiteSpace(declared))
        {
            return true;
        }

        var mediaType = declared.Split(';')[0].Trim();

        return mediaType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<(byte[] Head, Stream Content)> InspectAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        var seekable = content;

        if (!content.CanSeek)
        {
            seekable = new MemoryStream();
            await content.CopyToAsync(seekable, cancellationToken);
        }

        seekable.Position = 0;

        var head = new byte[HeadLength];
        var read = 0;

        while (read < head.Length)
        {
            var count = await seekable.ReadAsync(head.AsMemory(read, head.Length - read), cancellationToken);

            if (count == 0)
            {
                break;
            }

            read += count;
        }

        seekable.Position = 0;

        return (head[..read], seekable);
    }
}
