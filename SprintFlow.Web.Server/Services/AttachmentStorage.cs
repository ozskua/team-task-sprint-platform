using SprintFlow.Web.Server.Common;

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

    private readonly IObjectStorage objectStorage;
    private readonly ILogger<AttachmentStorage> logger;

    public AttachmentStorage(IObjectStorage objectStorage, ILogger<AttachmentStorage> logger)
    {
        this.objectStorage = objectStorage;
        this.logger = logger;
    }

    public string? ContentTypeFor(string extension)
        => ContentTypesByExtension.GetValueOrDefault(extension);

    public async Task<string> SaveAsync(
        int areaId,
        int boardId,
        int taskId,
        Stream content,
        long length,
        string extension,
        string contentType,
        CancellationToken cancellationToken)
    {
        var key = $"areas/{areaId}/boards/{boardId}/tasks/{taskId}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        await objectStorage.PutAsync(key, content, length, contentType, cancellationToken);

        return key;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
        => objectStorage.ExistsAsync(key, cancellationToken);

    public Task CopyToAsync(string key, Stream destination, CancellationToken cancellationToken)
        => objectStorage.CopyToAsync(key, destination, cancellationToken);

    public async Task TryDeleteAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await objectStorage.DeleteAsync(key, cancellationToken);
        }
        catch (ObjectStorageUnavailableException exception)
        {
            logger.LogWarning(
                exception,
                "Object '{Key}' could not be deleted from storage and is now orphaned.",
                key);
        }
    }
}
