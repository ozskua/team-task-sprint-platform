namespace SprintFlow.Web.Server.Services;

public interface IObjectStorage
{
    Task PutAsync(string key, Stream content, long length, string contentType, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);

    Task CopyToAsync(string key, Stream destination, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    Task EnsureBucketAsync(CancellationToken cancellationToken);
}
