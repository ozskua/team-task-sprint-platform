using SprintFlow.Web.Server.DTOs.Boards;

namespace SprintFlow.Web.Server.Interfaces.Services;

public interface IBoardDetailService
{
    /// <summary>
    /// Everything the board page needs, or <c>null</c> when no such board exists.
    /// </summary>
    Task<BoardDetailResponseDto?> GetAsync(int boardId, CancellationToken cancellationToken);
}
