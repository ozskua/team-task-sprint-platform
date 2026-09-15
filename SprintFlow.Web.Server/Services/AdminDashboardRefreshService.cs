namespace SprintFlow.Web.Server.Services;

public class AdminDashboardRefreshService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IAdminDashboardCache cache;
    private readonly ILogger<AdminDashboardRefreshService> logger;

    public AdminDashboardRefreshService(
        IServiceScopeFactory scopeFactory,
        IAdminDashboardCache cache,
        ILogger<AdminDashboardRefreshService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.cache = cache;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Admin dashboard figures will be refreshed every {Minutes} minutes.",
            RefreshInterval.TotalMinutes);

        await RefreshAsync(stoppingToken);

        using var timer = new PeriodicTimer(RefreshInterval);

        while (await SafeWaitAsync(timer, stoppingToken))
        {
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var dashboardService = scope.ServiceProvider.GetRequiredService<DashboardService>();

            var counts = await dashboardService.GetSystemCountsAsync(cancellationToken);

            await cache.SetAsync(counts, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "The admin dashboard figures could not be refreshed. The previous ones are kept, and the next attempt is in {Minutes} minutes.",
                RefreshInterval.TotalMinutes);
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
