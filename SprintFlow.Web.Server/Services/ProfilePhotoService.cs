using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;

namespace SprintFlow.Web.Server.Services;

public class ProfilePhotoService
{
    public const long MaxSizeBytes = 2 * 1024 * 1024;

    private const string KeyPrefix = "profile-photos";

    private static readonly Dictionary<string, string> ContentTypesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".webp"] = "image/webp"
        };

    private readonly UserRepository userRepository;
    private readonly IObjectStorage objectStorage;
    private readonly ILogger<ProfilePhotoService> logger;

    public ProfilePhotoService(
        UserRepository userRepository,
        IObjectStorage objectStorage,
        ILogger<ProfilePhotoService> logger)
    {
        this.userRepository = userRepository;
        this.objectStorage = objectStorage;
        this.logger = logger;
    }

    public async Task<UploadProfilePhotoOutcome> UploadAsync(
        int userId,
        string fileName,
        long sizeBytes,
        string? declaredContentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var leafName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(leafName) || sizeBytes <= 0)
        {
            return UploadProfilePhotoOutcome.FileMissing;
        }

        if (sizeBytes > MaxSizeBytes)
        {
            return UploadProfilePhotoOutcome.FileTooLarge;
        }

        var extension = Path.GetExtension(leafName);
        var contentType = ContentTypesByExtension.GetValueOrDefault(extension);

        if (contentType is null || !FileSignatures.DeclaredTypeIsConsistent(declaredContentType, contentType))
        {
            return UploadProfilePhotoOutcome.FileTypeNotAllowed;
        }

        var (head, seekableContent) = await FileSignatures.InspectAsync(content, cancellationToken);

        if (!FileSignatures.Matches(head, contentType))
        {
            return UploadProfilePhotoOutcome.FileTypeNotAllowed;
        }

        var user = await userRepository.GetForUpdateAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} is signed in but has no row.");

        var newKey = $"{KeyPrefix}/{userId}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        await objectStorage.PutAsync(newKey, seekableContent, sizeBytes, contentType, cancellationToken);

        var previousKey = user.ProfilePhotoKey;
        user.ProfilePhotoKey = newKey;

        try
        {
            await userRepository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteAsync(newKey, cancellationToken);

            throw;
        }

        if (previousKey is not null)
        {
            await TryDeleteAsync(previousKey, cancellationToken);
        }

        return UploadProfilePhotoOutcome.Uploaded;
    }

    public async Task RemoveAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetForUpdateAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} is signed in but has no row.");

        var previousKey = user.ProfilePhotoKey;

        if (previousKey is null)
        {
            return;
        }

        user.ProfilePhotoKey = null;

        await userRepository.SaveChangesAsync(cancellationToken);

        await TryDeleteAsync(previousKey, cancellationToken);
    }

    public async Task<(string Key, string ContentType)?> GetForDownloadAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var key = await userRepository.GetProfilePhotoKeyAsync(userId, cancellationToken);

        if (key is null)
        {
            return null;
        }

        var contentType = ContentTypesByExtension.GetValueOrDefault(Path.GetExtension(key));

        if (contentType is null || !await objectStorage.ExistsAsync(key, cancellationToken))
        {
            return null;
        }

        return (key, contentType);
    }

    public Task CopyToAsync(string key, Stream destination, CancellationToken cancellationToken)
        => objectStorage.CopyToAsync(key, destination, cancellationToken);

    private async Task TryDeleteAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await objectStorage.DeleteAsync(key, cancellationToken);
        }
        catch (ObjectStorageUnavailableException exception)
        {
            logger.LogWarning(
                exception,
                "Profile photo '{Key}' could not be deleted from storage and is now orphaned.",
                key);
        }
    }
}
