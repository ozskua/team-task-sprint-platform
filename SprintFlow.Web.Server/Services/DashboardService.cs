using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.AdminDashboard;
using SprintFlow.Web.Server.DTOs.Dashboard;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// The two dashboards: one user's own work, and the system-wide overview. Both are read-only
/// rollups over the same tables, and neither owns a repository.
/// </summary>
public class DashboardService
{
    private readonly TaskRepository taskRepository;
    private readonly UserRepository userRepository;
    private readonly BoardRepository boardRepository;
    private readonly IAdminDashboardCache dashboardCache;

    public DashboardService(
        TaskRepository taskRepository,
        UserRepository userRepository,
        BoardRepository boardRepository,
        IAdminDashboardCache dashboardCache)
    {
        this.taskRepository = taskRepository;
        this.userRepository = userRepository;
        this.boardRepository = boardRepository;
        this.dashboardCache = dashboardCache;
    }

    /// <summary>
    /// One user's dashboard, in two queries whatever the size of the account: their assigned tasks,
    /// and the members of the boards those tasks are on. Everything else is derived in memory.
    /// </summary>
    public async Task<DashboardResponseDto> GetForUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var assigned = await taskRepository.GetAssignedToUserAsync(userId, cancellationToken);

        // Every board the user holds a task on, in any column — holding a Cancelled task still
        // makes someone a member. This is the membership rule, stated once.
        var boardIds = assigned.Select(task => task.BoardId).Distinct().ToList();

        var membersByBoard = await taskRepository.GetMembersByBoardsAsync(boardIds, cancellationToken);

        return new DashboardResponseDto(
            CountUserBuckets(assigned),
            BuildTaskList(assigned),
            BuildTeamList(assigned, membersByBoard));
    }

    /// <summary>
    /// The system-wide overview, in three round trips: the tasks, and two scalar counts. No user
    /// row is loaded — the dashboard needs the number of accounts, not the directory.
    /// </summary>
    public async Task<AdminDashboardResponseDto> GetSystemAsync(CancellationToken cancellationToken)
    {
        // The figures are cached and refreshed on a timer, so they are usually served without
        // touching the database. A miss - including Redis being unreachable - computes them.
        var counts = await dashboardCache.GetAsync(cancellationToken);

        if (counts is null)
        {
            counts = await GetSystemCountsAsync(cancellationToken);

            await dashboardCache.SetAsync(counts, cancellationToken);
        }

        // The task list is deliberately not cached: it is the page's live detail rather than a
        // figure, and it is what the table below the cards draws.
        var tasks = await taskRepository.GetAllWithDetailsAsync(cancellationToken);

        return new AdminDashboardResponseDto(
            counts,
            tasks.Select(task => task.ToAdminDashboardTaskDto()).ToList());
    }

    /// <summary>
    /// The six system-wide figures, counted by the database.
    /// </summary>
    /// <remarks>
    /// Three queries, none of which loads a task row: one grouped count over the tasks joined to
    /// their columns, and a scalar count each for users and boards. This is the only place the
    /// figures are worked out — a dashboard request that misses the cache and the refresher that
    /// fills it both come through here, so the two can never disagree.
    /// </remarks>
    public async Task<AdminDashboardCountsDto> GetSystemCountsAsync(CancellationToken cancellationToken)
    {
        var byKind = await taskRepository.CountByColumnKindAsync(cancellationToken);
        var totalUsers = await userRepository.CountAsync(cancellationToken);
        var totalBoards = await boardRepository.CountAsync(cancellationToken);

        return new AdminDashboardCountsDto(
            CountFor(byKind, SystemColumnKind.ToDo),
            // A custom column is one with no system kind — Review, Testing, anything a board added.
            CountFor(byKind, null),
            CountFor(byKind, SystemColumnKind.Completed),
            CountFor(byKind, SystemColumnKind.Cancelled),
            totalUsers,
            totalBoards);
    }

    /// <summary>
    /// One kind's count, or zero when the database returned no row for it.
    /// </summary>
    /// <remarks>
    /// Each kind is asked for by name, so a kind added to the enum later is counted into none of
    /// the four rather than being folded silently into one of them.
    /// </remarks>
    private static int CountFor(IReadOnlyList<TaskColumnKindCount> counts, SystemColumnKind? kind)
        => counts.FirstOrDefault(count => count.SystemKind == kind)?.Count ?? 0;

    /// <summary>
    /// The four buckets. Every column falls into exactly one, so they always add up to the total.
    /// </summary>
    private static DashboardTaskCountsDto CountUserBuckets(IReadOnlyList<TaskItem> assigned)
        => new(
            CountKind(assigned, SystemColumnKind.ToDo),
            CountCustom(assigned),
            CountKind(assigned, SystemColumnKind.Completed),
            CountKind(assigned, SystemColumnKind.Cancelled));

    private static int CountKind(IReadOnlyList<TaskItem> tasks, SystemColumnKind kind)
        => tasks.Count(task => task.Column.SystemKind == kind);

    // A custom column is one with no system kind — Review, Testing, anything a board added.
    private static int CountCustom(IReadOnlyList<TaskItem> tasks)
        => tasks.Count(task => task.Column.SystemKind is null);

    /// <summary>
    /// The actionable tasks: To Do and the custom columns. Completed and Cancelled are left out
    /// because this answers "what is on my plate" — they are still in the counts, so nothing
    /// disappears. A display filter, never a state.
    /// </summary>
    private static IReadOnlyList<DashboardTaskDto> BuildTaskList(IReadOnlyList<TaskItem> assigned)
        => assigned
            .Where(task => task.Column.SystemKind is null or SystemColumnKind.ToDo)
            .Select(task => task.ToDashboardTaskDto())
            .ToList();

    /// <summary>
    /// One entry per board the caller holds a task on — deliberately not filtered the way the task
    /// list is, because a board where every assigned task is Completed is still a board they are on.
    /// </summary>
    private static IReadOnlyList<DashboardTeamDto> BuildTeamList(
        IReadOnlyList<TaskItem> assigned,
        IReadOnlyDictionary<int, IReadOnlyList<User>> membersByBoard)
        => assigned
            .GroupBy(task => task.BoardId)
            .Select(group => group.First().Board.ToDashboardTeamDto(
                group.Count(),
                membersByBoard.TryGetValue(group.Key, out var members) ? members : []))
            .ToList();
}
