using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SprintFlow.Web.Server.DTOs.Boards;
using StackExchange.Redis;

namespace SprintFlow.Web.Server.Services;

public class BoardSummaryCache : IBoardSummaryCache
{
    private const string KeyPrefix = "board-summary:v1:";

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

            return null;
        }

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
            logger.LogWarning(
                exception,
                "Redis was unreachable while invalidating the cached summary for board {BoardId}. It will expire on its own.",
                boardId);
        }
    }

    private static string Key(int boardId) => $"{KeyPrefix}{boardId}";

    private static bool IsRedisUnavailable(Exception exception)
        => exception is RedisException or TimeoutException;
}
