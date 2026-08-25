using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Dashboard;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Assembles one user's dashboard. Like <see cref="BoardDetailService"/> it owns no repository of
/// its own — it reads through two existing ones and decides what the answer means.
/// </summary>
/// <remarks>
/// Two queries, whatever the size of the account. One reads the caller's assigned tasks; the other
/// reads the members of the boards those tasks are on. Everything else — the four counts, the task
/// list, the team list and the per-board totals — is worked out from the first result set, so no
/// part of this fans out per board.
/// </remarks>
public class DashboardService : IDashboardService
{
    private readonly ITaskRepository taskRepository;
    private readonly ITaskParticipantRepository participantRepository;

    public DashboardService(
        ITaskRepository taskRepository,
        ITaskParticipantRepository participantRepository)
    {
        this.taskRepository = taskRepository;
        this.participantRepository = participantRepository;
    }

    public async Task<DashboardResponseDto> GetForUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var assigned = await taskRepository.GetAssignedToUserAsync(userId, cancellationToken);

        // Every board the user holds a task on — in any column, because holding a Cancelled task
        // still makes someone a member. This is the membership rule, stated once.
        var boardIds = assigned
            .Select(task => task.BoardId)
            .Distinct()
            .ToList();

        // A second query rather than part of the graph above, because membership is not a
        // relationship a board has — it is worked out from who is assigned its tasks.
        var membersByBoard = await participantRepository.GetMembersByBoardsAsync(
            boardIds,
            cancellationToken);

        return new DashboardResponseDto(
            CountByBucket(assigned),
            BuildTaskList(assigned),
            BuildTeamList(assigned, membersByBoard));
    }

    /// <summary>
    /// The four buckets. Every column a board can have falls into exactly one of them, so these
    /// always add up to the caller's total assigned tasks.
    /// </summary>
    private static DashboardTaskCountsDto CountByBucket(IReadOnlyList<TaskItem> assigned)
        => new(
            assigned.Count(task => task.Column.SystemKind == SystemColumnKind.ToDo),
            // A custom column is one with no system kind — Review, Testing, Blocked, anything the
            // board's own people added.
            assigned.Count(task => task.Column.SystemKind is null),
            assigned.Count(task => task.Column.SystemKind == SystemColumnKind.Completed),
            assigned.Count(task => task.Column.SystemKind == SystemColumnKind.Cancelled));

    /// <summary>
    /// The actionable tasks: To Do and the custom columns. Completed and Cancelled are left out
    /// because this list answers "what is on my plate" — they are still reported in the counts, so
    /// nothing disappears. A display filter, never a state: a task can move back out of either.
    /// </summary>
    private static IReadOnlyList<DashboardTaskDto> BuildTaskList(IReadOnlyList<TaskItem> assigned)
        => assigned
            .Where(task => task.Column.SystemKind is null or SystemColumnKind.ToDo)
            .Select(task => task.ToDashboardTaskDto())
            .ToList();

    /// <summary>
    /// One entry per board the caller holds a task on. Deliberately not filtered the way the task
    /// list is — a board where every assigned task is Completed is still a board they are on.
    /// </summary>
    private static IReadOnlyList<DashboardTeamDto> BuildTeamList(
        IReadOnlyList<TaskItem> assigned,
        IReadOnlyDictionary<int, IReadOnlyList<User>> membersByBoard)
        => assigned
            .GroupBy(task => task.BoardId)
            .Select(group => group.First().Board.ToDashboardTeamDto(
                // Every column counts here, which is why this can exceed the number of rows the
                // same board contributes to the task list above.
                group.Count(),
                membersByBoard.TryGetValue(group.Key, out var members) ? members : []))
            // The tasks arrive area-then-board ordered, and GroupBy preserves first appearance, so
            // the cards come out in the same order as the list above them.
            .ToList();
}
