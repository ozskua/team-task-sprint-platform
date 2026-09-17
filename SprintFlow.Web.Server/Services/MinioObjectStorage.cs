using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Options;

namespace SprintFlow.Web.Server.Services;

public class MinioObjectStorage : IObjectStorage
{
    private readonly IMinioClient client;
    private readonly string bucketName;

    public MinioObjectStorage(IOptions<MinioOptions> options)
    {
        var minio = options.Value;

        client = new MinioClient()
            .WithEndpoint(minio.Endpoint)
            .WithCredentials(minio.AccessKey, minio.SecretKey)
            .WithSSL(minio.UseSsl)
            .Build();

        bucketName = minio.BucketName;
    }

    public async Task PutAsync(
        string key,
        Stream content,
        long length,
        string contentType,
        CancellationToken cancellationToken)
    {
        var args = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(key)
            .WithStreamData(content)
            .WithObjectSize(length)
            .WithContentType(contentType);

        await Guard(() => client.PutObjectAsync(args, cancellationToken), key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        var args = new StatObjectArgs()
            .WithBucket(bucketName)
            .WithObject(key);

        try
        {
            await Guard(() => client.StatObjectAsync(args, cancellationToken), key);

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    public Task CopyToAsync(string key, Stream destination, CancellationToken cancellationToken)
    {
        var args = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(key)
            .WithCallbackStream(async (stream, token) => await stream.CopyToAsync(destination, token));

        return Guard(() => client.GetObjectAsync(args, cancellationToken), key);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(key);

        return Guard(() => client.RemoveObjectAsync(args, cancellationToken), key);
    }

    public async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        var exists = await Guard(
            () => client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), cancellationToken),
            bucketName);

        if (!exists)
        {
            await Guard(
                () => client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), cancellationToken),
                bucketName);
        }
    }

    private static async Task<T> Guard<T>(Func<Task<T>> operation, string key)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            throw new ObjectStorageUnavailableException($"Object storage is unavailable for '{key}'.", exception);
        }
    }

    private static async Task Guard(Func<Task> operation, string key)
    {
        try
        {
            await operation();
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            throw new ObjectStorageUnavailableException($"Object storage is unavailable for '{key}'.", exception);
        }
    }

    private static bool IsUnavailable(Exception exception)
        => exception is HttpRequestException
            || exception is System.Net.Sockets.SocketException
            || (exception is MinioException
                && exception is not ObjectNotFoundException
                && exception is not BucketNotFoundException
                && exception is not AccessDeniedException
                && exception is not AuthorizationException);
}
