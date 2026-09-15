using SprintFlow.Web.Server.DTOs.Boards;

namespace SprintFlow.Web.Server.Services;

public interface IBoardSummaryCache
{
    Task<BoardSummaryResponseDto?> GetAsync(int boardId, CancellationToken cancellationToken);

    Task SetAsync(int boardId, BoardSummaryResponseDto summary, CancellationToken cancellationToken);

    Task RemoveAsync(int boardId, CancellationToken cancellationToken);
}
