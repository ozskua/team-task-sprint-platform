namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Recomputes the admin dashboard's figures from SQL Server every five minutes and puts them in
/// Redis, so a dashboard request rarely has to count anything itself.
/// </summary>
/// <remarks>
/// This is the only thing that writes the entry. There is deliberately no invalidation anywhere
/// else: unlike the board summary, which is dropped by each write that changes it, these figures
/// cover the whole system, so every task change anywhere would drop them and the cache would almost
/// never be warm. Bounded staleness is the trade this makes on purpose.
/// <para>
/// A hosted service is a singleton while <see cref="DashboardService"/> and the repositories under
/// it are scoped, so a scope is created per turn rather than injecting them here — a DbContext must
/// not be shared across turns.
/// </para>
/// </remarks>
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

        // Once before the first wait, so the dashboard is served from the cache straight away
        // rather than after the first interval.
        await RefreshAsync(stoppingToken);

        using var timer = new PeriodicTimer(RefreshInterval);

        // One turn at a time: the next tick is only awaited after this one has finished, so two
        // refreshes can never overlap. Nothing here needs a lock.
        while (await SafeWaitAsync(timer, stoppingToken))
        {
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            // A scope per turn: DashboardService and its repositories are scoped, and the
            // DbContext beneath them is not safe to reuse across turns.
            await using var scope = scopeFactory.CreateAsyncScope();

            var dashboardService = scope.ServiceProvider.GetRequiredService<DashboardService>();

            var counts = await dashboardService.GetSystemCountsAsync(cancellationToken);

            await cache.SetAsync(counts, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down, not a failure.
            throw;
        }
        catch (Exception exception)
        {
            // Caught so one bad turn does not end the loop. The cache is deliberately left alone:
            // figures that could not be recomputed are no reason to throw away the ones already
            // there, which are still readable and will expire on their own.
            logger.LogWarning(
                exception,
                "The admin dashboard figures could not be refreshed. The previous ones are kept, and the next attempt is in {Minutes} minutes.",
                RefreshInterval.TotalMinutes);
        }
    }

    /// <summary>Waits for the next tick, treating shutdown as "stop" rather than as an error.</summary>
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
