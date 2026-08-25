using SprintFlow.Web.Server.DTOs.Boards;

namespace SprintFlow.Web.Server.Interfaces.Services;

/// <summary>
/// One board's own figures. Nothing here spans boards.
/// </summary>
/// <remarks>
/// Separate from <c>IAdminDashboardService</c> on purpose: that one answers "what is the state of
/// the system" and this one answers "what is the state of this board". Sharing an implementation
/// would mean one class doing both, and the two would drift into each other's scope.
/// </remarks>
public interface IBoardSummaryService
{
    /// <summary>The board's figures, or <c>null</c> when no such board exists.</summary>
    Task<BoardSummaryResponseDto?> GetAsync(int boardId, CancellationToken cancellationToken);
}
