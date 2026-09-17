using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class SprintMappings
{
    public static SprintResponseDto ToResponseDto(
        this Sprint sprint,
        SprintStatus status,
        int taskCount)
        => new(
            sprint.SprintId,
            sprint.BoardId,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            status.ToString(),
            taskCount,
            sprint.CreatedByUserId,
            sprint.CreatedByUser.FullName,
            sprint.CreatedDate);

    public static SprintListItemResponseDto ToListItemResponseDto(
        this Sprint sprint,
        SprintStatus status,
        int taskCount)
        => new(
            sprint.SprintId,
            sprint.BoardId,
            sprint.Board.Name,
            sprint.Board.AreaId,
            sprint.Board.Area.Name,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            status.ToString(),
            taskCount);

    public static SprintTaskResponseDto ToSprintTaskResponseDto(this TaskItem task)
        => new(
            task.TaskId,
            task.Title,
            task.Column.Name,
            task.Column.SystemKind?.ToString(),
            task.Participants.Select(participant => (int?)participant.UserId).FirstOrDefault(),
            task.Participants.Select(participant => participant.User.FullName).FirstOrDefault(),
            task.CreatedByUser.FullName,
            task.CreatedDate);

    public static SprintReportRowDto ToReportRowDto(
        this Sprint sprint,
        SprintStatus status,
        SprintTaskOutcome outcome)
        => new(
            sprint.SprintId,
            sprint.BoardId,
            sprint.Board.Name,
            sprint.Board.AreaId,
            sprint.Board.Area.Name,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            status.ToString(),
            outcome.Total,
            outcome.CompletedCount,
            outcome.CompletedPercent,
            outcome.CancelledCount,
            outcome.CancelledPercent,
            outcome.OngoingCount,
            outcome.OngoingPercent);

    public static SprintDetailResponseDto ToDetailResponseDto(
        this Sprint sprint,
        SprintStatus status,
        int taskCount)
        => new(
            sprint.SprintId,
            sprint.BoardId,
            sprint.Board.Name,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            status.ToString(),
            taskCount,
            sprint.CreatedByUserId,
            sprint.CreatedByUser.FullName,
            sprint.CreatedDate);
}
