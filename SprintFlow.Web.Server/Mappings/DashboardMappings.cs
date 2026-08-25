using SprintFlow.Web.Server.DTOs.Dashboard;
using SprintFlow.Web.Server.DTOs.Teams;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class DashboardMappings
{
    /// <summary>
    /// Requires the task's board, that board's area, and the task's column to be loaded.
    /// </summary>
    /// <remarks>
    /// The column's kind travels as its name rather than its number, so a client reads
    /// <c>"Completed"</c> instead of <c>2</c> — the same rule the board endpoint follows.
    /// </remarks>
    public static DashboardTaskDto ToDashboardTaskDto(this TaskItem task)
        => new(
            task.TaskId,
            task.Title,
            task.BoardId,
            task.Board.Name,
            task.Board.AreaId,
            task.Board.Area.Name,
            task.TaskColumnId,
            task.Column.Name,
            task.Column.SystemKind?.ToString());

    /// <summary>
    /// Requires the board's area to be loaded.
    /// </summary>
    /// <param name="board">The board the team belongs to, with its area loaded.</param>
    /// <param name="myAssignedTaskCount">
    /// The caller's tasks on this board across every column. Passed in rather than counted here,
    /// because which tasks belong to the caller is the service's question, not the mapper's.
    /// </param>
    /// <param name="members">
    /// Computed from assignment and passed in, because membership is not part of the board's object
    /// graph — there is no membership table to include. Already distinct per user.
    /// </param>
    public static DashboardTeamDto ToDashboardTeamDto(
        this Board board,
        int myAssignedTaskCount,
        IReadOnlyList<User> members)
        => new(
            board.BoardId,
            board.Name,
            board.AreaId,
            board.Area.Name,
            myAssignedTaskCount,
            members
                .Select(member => new TeamMemberResponseDto(member.UserId, member.FullName))
                .ToList());
}
