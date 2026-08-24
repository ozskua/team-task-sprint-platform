using SprintFlow.Web.Server.DTOs.AdminDashboard;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class AdminDashboardMappings
{
    /// <summary>
    /// Requires the task's board, that board's area, its column, its creator and its participants
    /// (with their user) to be loaded.
    /// </summary>
    /// <remarks>
    /// The column's kind travels as its name rather than its number, so a client reads
    /// <c>"Completed"</c> instead of <c>2</c> — the same rule the board endpoint follows.
    /// </remarks>
    public static AdminDashboardTaskDto ToAdminDashboardTaskDto(this TaskItem task)
        => new(
            task.TaskId,
            task.Title,
            task.BoardId,
            task.Board.Name,
            task.Board.AreaId,
            task.Board.Area.Name,
            task.TaskColumnId,
            task.Column.Name,
            task.Column.SystemKind?.ToString(),
            task.CreatedByUser.FullName,
            task.CreatedDate,
            // Zero or one: the unique index on TaskId makes a second assignee unstorable, so the
            // first row is the assignee and its absence means nobody is answerable yet.
            task.Participants
                .Select(p => new TaskAssigneeResponseDto(p.UserId, p.User.FullName))
                .FirstOrDefault());
}
