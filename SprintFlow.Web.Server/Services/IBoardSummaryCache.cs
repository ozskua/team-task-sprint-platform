using SprintFlow.Web.Server.DTOs.Boards;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Where one board's computed figures are kept between reads.
/// </summary>
/// <remarks>
/// An optimisation over the database, never a second source of truth: every method here may do
/// nothing at all - because Redis is unreachable, or the board was never cached - and the caller
/// still gets the right answer by computing it. It is an interface because it is the seam between
/// the application and Redis, and because seven call sites must agree on one key, one payload
/// format and one expiry.
/// </remarks>
public interface IBoardSummaryCache
{
    /// <summary>The cached figures, or null when nothing is cached - including Redis being down.</summary>
    Task<BoardSummaryResponseDto?> GetAsync(int boardId, CancellationToken cancellationToken);

    Task SetAsync(int boardId, BoardSummaryResponseDto summary, CancellationToken cancellationToken);

    /// <summary>Drops the entry so the next read recomputes it.</summary>
    Task RemoveAsync(int boardId, CancellationToken cancellationToken);
}
