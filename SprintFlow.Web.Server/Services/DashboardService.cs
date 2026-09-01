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

    public DashboardService(
        TaskRepository taskRepository,
        UserRepository userRepository,
        BoardRepository boardRepository)
    {
        this.taskRepository = taskRepository;
        this.userRepository = userRepository;
        this.boardRepository = boardRepository;
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
        var tasks = await taskRepository.GetAllWithDetailsAsync(cancellationToken);
        var totalUsers = await userRepository.CountAsync(cancellationToken);
        var totalBoards = await boardRepository.CountAsync(cancellationToken);

        return new AdminDashboardResponseDto(
            new AdminDashboardCountsDto(
                CountKind(tasks, SystemColumnKind.ToDo),
                CountCustom(tasks),
                CountKind(tasks, SystemColumnKind.Completed),
                CountKind(tasks, SystemColumnKind.Cancelled),
                totalUsers,
                totalBoards),
            tasks.Select(task => task.ToAdminDashboardTaskDto()).ToList());
    }

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
