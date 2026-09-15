using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

public class AttachmentStorage
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv",
            [".md"] = "text/markdown",
            [".zip"] = "application/zip"
        };

    private readonly string rootPath;

    private readonly string rootPrefix;

    public AttachmentStorage(IOptions<AttachmentOptions> options)
    {
        rootPath = Path.GetFullPath(options.Value.RootPath);
        rootPrefix = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;

        Directory.CreateDirectory(rootPath);
    }

    public string? ContentTypeFor(string extension)
        => ContentTypesByExtension.GetValueOrDefault(extension);

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        var storedName = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();

        await using var file = File.Create(PathOf(storedName));

        await content.CopyToAsync(file, cancellationToken);

        return storedName;
    }

    public Stream? Open(string storedName)
    {
        var path = PathOf(storedName);

        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public void Delete(string storedName) => File.Delete(PathOf(storedName));

    private string PathOf(string storedName)
    {
        var path = Path.GetFullPath(Path.Combine(rootPath, storedName));

        return path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
            ? path
            : throw new InvalidOperationException(
                "An attachment path resolved outside the attachment folder.");
    }
}
