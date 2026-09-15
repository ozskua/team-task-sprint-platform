using SprintFlow.Web.Server.DTOs.Dashboard;
using SprintFlow.Web.Server.DTOs.Teams;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class DashboardMappings
{
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
