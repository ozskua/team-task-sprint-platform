using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

/// <summary>
/// Hand-written entity to DTO mapping, kept in one place so what leaves the server is visible.
/// </summary>
/// <remarks>
/// The status and the task count are passed in rather than worked out here: neither is on the
/// entity, one depends on today's date and the other on a separate query. Deciding what they are is
/// the service's job, and assembling the response is this one's — the same split
/// <c>BoardMappings.ToDetailResponseDto</c> uses for team members.
/// </remarks>
public static class SprintMappings
{
    /// <summary>Requires <c>Include(s =&gt; s.CreatedByUser)</c>.</summary>
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
            // As a string, like every other enum that leaves this server: no JSON options are
            // configured, so the raw value would serialise as a number and mean nothing to a client.
            status.ToString(),
            taskCount,
            sprint.CreatedByUserId,
            sprint.CreatedByUser.FullName,
            sprint.CreatedDate);

    /// <summary>
    /// A row of the system-wide list. Requires <c>Include(s =&gt; s.Board).ThenInclude(b =&gt; b.Area)</c>.
    /// </summary>
    /// <remarks>
    /// The creator is deliberately absent: the system-wide list groups by board and answers "what is
    /// scheduled where", and a column of names would be noise on a page nobody manages from.
    /// </remarks>
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

    /// <summary>
    /// One task of a sprint. Requires the column, creator and assignment rows to be loaded.
    /// </summary>
    public static SprintTaskResponseDto ToSprintTaskResponseDto(this TaskItem task)
        => new(
            task.TaskId,
            task.Title,
            task.Column.Name,
            // As a string, like every other enum that leaves this server; null for a custom column.
            task.Column.SystemKind?.ToString(),
            // At most one row exists — the unique index on TaskId sees to that — so the first is the
            // assignee, and no row means unassigned.
            task.Participants.Select(participant => participant.User.FullName).FirstOrDefault(),
            task.CreatedByUser.FullName,
            task.CreatedDate);

    /// <summary>
    /// A row of the admin sprint report. Requires the same includes as the system-wide list.
    /// </summary>
    /// <remarks>
    /// The outcome is passed in for the same reason the task count is: it comes from a second query,
    /// and working out what it means is the service's job.
    /// </remarks>
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

    /// <summary>Requires <c>Include(s =&gt; s.Board)</c> and <c>Include(s =&gt; s.CreatedByUser)</c>.</summary>
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
