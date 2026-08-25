using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Boards;

namespace SprintFlow.Web.Server.Interfaces.Services;

public interface IBoardDetailService
{
    /// <summary>
    /// The whole board page: the board, its computed team members, and its columns with whichever
    /// of its tasks the caller asked for.
    /// </summary>
    /// <param name="sprintId">
    /// Show only this sprint's tasks. Must be one of the board's. Cannot be combined with
    /// <paramref name="allTasks"/>.
    /// </param>
    /// <param name="allTasks">
    /// Show every task on the board regardless of sprint — what the backlog needs, since it is the
    /// board's complete inventory.
    /// </param>
    /// <remarks>
    /// With neither, the board's sprint running today is used. If none is running the page comes
    /// back with its columns and no tasks, and <c>SelectedSprint</c> is null — an empty kanban is
    /// the honest answer for a board with nothing in progress.
    /// <para>
    /// The set of columns is the same in every case. There is one kanban per board.
    /// </para>
    /// </remarks>
    Task<(GetBoardDetailOutcome Outcome, BoardDetailResponseDto? Board)> GetAsync(
        int boardId,
        int? sprintId,
        bool allTasks,
        CancellationToken cancellationToken);
}
