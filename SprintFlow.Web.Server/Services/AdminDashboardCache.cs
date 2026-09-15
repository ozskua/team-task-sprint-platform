using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SprintFlow.Web.Server.DTOs.AdminDashboard;
using StackExchange.Redis;

namespace SprintFlow.Web.Server.Services;

public class AdminDashboardCache : IAdminDashboardCache
{
    private const string Key = "admin-dashboard:v1";

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

            return null;
        }

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
            logger.LogWarning(
                exception,
                "Redis was unreachable while caching the admin dashboard figures.");
        }
    }

    private static bool IsRedisUnavailable(Exception exception)
        => exception is RedisException or TimeoutException;
}
