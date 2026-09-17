using SprintFlow.Web.Server.Common;

namespace SprintFlow.Web.Server.Services;

public class MinioBucketInitializer : IHostedService
{
    private readonly IObjectStorage objectStorage;
    private readonly ILogger<MinioBucketInitializer> logger;

    public MinioBucketInitializer(IObjectStorage objectStorage, ILogger<MinioBucketInitializer> logger)
    {
        this.objectStorage = objectStorage;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await objectStorage.EnsureBucketAsync(cancellationToken);

            logger.LogInformation("Object storage bucket is ready.");
        }
        catch (ObjectStorageUnavailableException exception)
        {
            logger.LogWarning(
                exception,
                "Object storage was unreachable at start-up. Uploads and downloads will fail until it is back.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
