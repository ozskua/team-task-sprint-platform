using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SprintFlow.Web.Server.DTOs.AdminDashboard;
using StackExchange.Redis;

namespace SprintFlow.Web.Server.Services;

// The system's six figures, kept in Redis between reads. The dashboard is system-wide and
// identical for every caller, so one entry serves all of them and the key carries no user id.
public class AdminDashboardCache : IAdminDashboardCache
{
    // The v1 is the shape of AdminDashboardCountsDto: it is a positional record, so an entry
    // written by an older build would deserialise with an added field silently zero. Bump it when
    // the record changes and the old entries become unreachable instead of misread.
    private const string Key = "admin-dashboard:v1";

    // Longer than the five minutes between refreshes, so a late refresh does not leave the
    // dashboard without an entry. Absolute, not sliding: a sliding window would be renewed by the
    // very reads that should eventually notice nothing is refreshing it.
    private static readonly DistributedCacheEntryOptions EntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(6)
    };

    private readonly IDistributedCache cache;
    private readonly ILogger<AdminDashboardCache> logger;

    public AdminDashboardCache(IDistributedCache cache, ILogger<AdminDashboardCache> logger)
    {
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<AdminDashboardCountsDto?> GetAsync(CancellationToken cancellationToken)
    {
        byte[]? cached;

        try
        {
            cached = await cache.GetAsync(Key, cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            logger.LogWarning(
                exception,
                "Redis was unreachable while reading the cached admin dashboard figures. Computing them from the database instead.");

            // A miss, which is the truth: the caller recomputes and returns the same numbers.
            return null;
        }

        // Not guarded: a payload that fails to deserialise is a defect here or a key that outlived
        // its shape, and hiding it behind a recompute would leave the cache quietly useless.
        return cached is null ? null : JsonSerializer.Deserialize<AdminDashboardCountsDto>(cached);
    }

    public async Task SetAsync(AdminDashboardCountsDto counts, CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetAsync(
                Key,
                JsonSerializer.SerializeToUtf8Bytes(counts),
                EntryOptions,
                cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            // Failing to cache costs one recomputation and nothing else: the caller already has
            // the figures, and the refresher will try again on its next turn.
            logger.LogWarning(
                exception,
                "Redis was unreachable while caching the admin dashboard figures.");
        }
    }

    // Narrow on purpose: Redis being down is survivable, but a defect here must keep travelling to
    // the exception middleware. Two types because RedisTimeoutException derives from
    // TimeoutException, not RedisException.
    private static bool IsRedisUnavailable(Exception exception)
        => exception is RedisException or TimeoutException;
}
