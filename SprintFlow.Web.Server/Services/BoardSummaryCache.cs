using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SprintFlow.Web.Server.DTOs.Boards;
using StackExchange.Redis;

namespace SprintFlow.Web.Server.Services;

// One board's figures, kept in Redis between reads. The same board produces the same figures for
// every caller, so one entry per board serves all of them.
public class BoardSummaryCache : IBoardSummaryCache
{
    // The v1 is the shape of BoardSummaryResponseDto: it is a positional record, so an entry
    // written by an older build would deserialise with an added field silently zero. Bump it when
    // the record changes and the old entries become unreachable instead of misread.
    private const string KeyPrefix = "board-summary:v1:";

    // Mutations drop the entry, so this only backstops what no mutation covers: the calendar (a
    // sprint becomes active at midnight with nothing written) and an invalidation that could not
    // reach Redis. Absolute, not sliding, or an open board would keep renewing it.
    private static readonly DistributedCacheEntryOptions EntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };

    private readonly IDistributedCache cache;
    private readonly ILogger<BoardSummaryCache> logger;

    public BoardSummaryCache(IDistributedCache cache, ILogger<BoardSummaryCache> logger)
    {
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<BoardSummaryResponseDto?> GetAsync(int boardId, CancellationToken cancellationToken)
    {
        byte[]? cached;

        try
        {
            cached = await cache.GetAsync(Key(boardId), cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            logger.LogWarning(
                exception,
                "Redis was unreachable while reading the cached summary for board {BoardId}. Computing it from the database instead.",
                boardId);

            // A miss, which is the truth: the caller recomputes and returns the same numbers.
            return null;
        }

        // Not guarded: a payload that fails to deserialise is a defect here or a key that outlived
        // its shape, and hiding it behind a recompute would leave the cache quietly useless.
        return cached is null ? null : JsonSerializer.Deserialize<BoardSummaryResponseDto>(cached);
    }

    public async Task SetAsync(
        int boardId,
        BoardSummaryResponseDto summary,
        CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetAsync(
                Key(boardId),
                JsonSerializer.SerializeToUtf8Bytes(summary),
                EntryOptions,
                cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            // Failing to cache costs one recomputation and nothing else.
            logger.LogWarning(
                exception,
                "Redis was unreachable while caching the summary for board {BoardId}.",
                boardId);
        }
    }

    public async Task RemoveAsync(int boardId, CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync(Key(boardId), cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            // Warned rather than thrown: the write this follows is already committed, so throwing
            // would report a failure that did not happen. The entry it could not drop is stale only
            // until the one-minute expiry, never indefinitely.
            logger.LogWarning(
                exception,
                "Redis was unreachable while invalidating the cached summary for board {BoardId}. It will expire on its own.",
                boardId);
        }
    }

    private static string Key(int boardId) => $"{KeyPrefix}{boardId}";

    // Narrow on purpose: Redis being down is survivable, but a defect here must keep travelling to
    // the exception middleware. Two types because RedisTimeoutException derives from
    // TimeoutException, not RedisException.
    private static bool IsRedisUnavailable(Exception exception)
        => exception is RedisException or TimeoutException;
}
