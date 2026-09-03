using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// The attachment folder on disk. The database holds the metadata row; the bytes live here.
/// </summary>
/// <remarks>
/// Its own class because it is the only thing in the application that touches the filesystem, and
/// because it owns the two rules that stop an upload becoming a path: the file is stored under a
/// name generated here, and only the extensions listed here are accepted at all.
/// </remarks>
public class AttachmentStorage
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    // Doubles as the allowlist: an extension that is not a key here is refused. The content type is
    // taken from the extension rather than from the upload, so a client cannot label a file as
    // something it is not.
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

    /// <summary>The folder plus a separator, so a sibling folder cannot match it as a prefix.</summary>
    private readonly string rootPrefix;

    public AttachmentStorage(IOptions<AttachmentOptions> options)
    {
        rootPath = Path.GetFullPath(options.Value.RootPath);
        rootPrefix = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;

        // Idempotent, so a fresh machine needs the setting but not the folder.
        Directory.CreateDirectory(rootPath);
    }

    /// <summary>The content type for an accepted extension, or <c>null</c> when it is not one.</summary>
    public string? ContentTypeFor(string extension)
        => ContentTypesByExtension.GetValueOrDefault(extension);

    /// <summary>Writes the bytes under a name generated here, and returns that name.</summary>
    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        // A GUID, never anything the uploader chose, so nothing they send can be read as a path.
        var storedName = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();

        await using var file = File.Create(PathOf(storedName));

        await content.CopyToAsync(file, cancellationToken);

        return storedName;
    }

    /// <summary><c>null</c> when the row survives but its file does not.</summary>
    public Stream? Open(string storedName)
    {
        var path = PathOf(storedName);

        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    /// <summary>
    /// Removes the file. An already-missing file is not an error: the metadata row is the record,
    /// and there would be nothing left to do.
    /// </summary>
    public void Delete(string storedName) => File.Delete(PathOf(storedName));

    private string PathOf(string storedName)
    {
        var path = Path.GetFullPath(Path.Combine(rootPath, storedName));

        // Defence in depth. Stored names are generated above, so this cannot fire today — it is what
        // stops a corrupted row from reaching a file outside the folder.
        return path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
            ? path
            : throw new InvalidOperationException(
                "An attachment path resolved outside the attachment folder.");
    }
}
