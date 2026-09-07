using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SprintFlow.Web.Server.DTOs.AdminDashboard;
using StackExchange.Redis;

namespace SprintFlow.Web.Server.Services;

// The system's six figures, kept in Redis between reads. Nothing here is user-specific: the
// dashboard is system-wide and identical for every signed-in caller, so one entry serves all of
// them and the key carries no user id.
public class AdminDashboardCache : IAdminDashboardCache
{
    // Full name in Redis is sprintflow:admin-dashboard:v1. The v1 is the shape of
    // AdminDashboardCountsDto: it is a positional record, so an entry written by an older build
    // would deserialise into a new shape with the added field silently zero. Bump it when the
    // record changes, and the old entries become unreachable instead of misread.
    private const string Key = "admin-dashboard:v1";

    // Longer than the five minutes between refreshes, so a refresh running a little late does not
    // leave the dashboard without an entry. It exists at all so that a refresher which stops
    // running cannot leave figures in Redis for ever. Absolute, not sliding - a sliding window
    // would be renewed by the very reads that should eventually notice nothing is refreshing it.
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

            // Reported as a miss, which is the truth: there is no cached answer available. The
            // caller computes the figures and returns the same numbers it always would.
            return null;
        }

        // Not guarded. A payload that fails to deserialise is a defect in this class or a key that
        // outlived its shape, and hiding it behind a recompute would leave the cache quietly
        // useless with nothing in the log to say so.
        return cached is null ? null : JsonSerializer.Deserialize<AdminDashboardCountsDto>(cached);
    }

    public async Task SetAsync(AdminDashboardCountsDto counts, CancellationToken cancellationToken)
    {
        try
        {
            // Straight to UTF-8 bytes, which is what IDistributedCache stores — going through a
            // string would encode the same data twice.
            await cache.SetAsync(
                Key,
                JsonSerializer.SerializeToUtf8Bytes(counts),
                EntryOptions,
                cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            // Failing to cache costs a recomputation on the next read and nothing else. Whoever
            // called this already has the figures: a request is about to return them, and the
            // refresher will try again on its next turn.
            logger.LogWarning(
                exception,
                "Redis was unreachable while caching the admin dashboard figures.");
        }
    }

    // Narrow on purpose: Redis being down is expected and survivable, but a serialisation defect
    // or a bug here must keep travelling to the exception middleware. Two types because they share
    // no base - RedisTimeoutException derives from TimeoutException, not RedisException.
    private static bool IsRedisUnavailable(Exception exception)
        => exception is RedisException or TimeoutException;
}
