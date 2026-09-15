using SprintFlow.Web.Server.DTOs.AdminDashboard;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class AdminDashboardMappings
{
    public static AdminDashboardTaskDto ToAdminDashboardTaskDto(this TaskItem task) => new (

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

            task.Participants.Select(p => new TaskAssigneeResponseDto(p.UserId, p.User.FullName)).FirstOrDefault());
}
