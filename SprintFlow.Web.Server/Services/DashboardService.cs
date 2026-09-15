using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.AdminDashboard;
using SprintFlow.Web.Server.DTOs.Dashboard;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

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

    public async Task<DashboardResponseDto> GetForUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var assigned = await taskRepository.GetAssignedToUserAsync(userId, cancellationToken);

        var boardIds = assigned.Select(task => task.BoardId).Distinct().ToList();

        var membersByBoard = await taskRepository.GetMembersByBoardsAsync(boardIds, cancellationToken);

        return new DashboardResponseDto(
            CountUserBuckets(assigned),
            BuildTaskList(assigned),
            BuildTeamList(assigned, membersByBoard));
    }

    public async Task<AdminDashboardResponseDto> GetSystemAsync(CancellationToken cancellationToken)
    {
        var counts = await dashboardCache.GetAsync(cancellationToken);

        if (counts is null)
        {
            counts = await GetSystemCountsAsync(cancellationToken);

            await dashboardCache.SetAsync(counts, cancellationToken);
        }

        var tasks = await taskRepository.GetAllWithDetailsAsync(cancellationToken);

        return new AdminDashboardResponseDto(
            counts,
            tasks.Select(task => task.ToAdminDashboardTaskDto()).ToList());
    }

    public async Task<AdminDashboardCountsDto> GetSystemCountsAsync(CancellationToken cancellationToken)
    {
        var byKind = await taskRepository.CountByColumnKindAsync(cancellationToken);
        var totalUsers = await userRepository.CountAsync(cancellationToken);
        var totalBoards = await boardRepository.CountAsync(cancellationToken);

        return new AdminDashboardCountsDto(
            CountFor(byKind, SystemColumnKind.ToDo),
            CountFor(byKind, null),
            CountFor(byKind, SystemColumnKind.Completed),
            CountFor(byKind, SystemColumnKind.Cancelled),
            totalUsers,
            totalBoards);
    }

    private static int CountFor(IReadOnlyList<TaskColumnKindCount> counts, SystemColumnKind? kind)
        => counts.FirstOrDefault(count => count.SystemKind == kind)?.Count ?? 0;

    private static DashboardTaskCountsDto CountUserBuckets(IReadOnlyList<TaskItem> assigned)
        => new(
            CountKind(assigned, SystemColumnKind.ToDo),
            CountCustom(assigned),
            CountKind(assigned, SystemColumnKind.Completed),
            CountKind(assigned, SystemColumnKind.Cancelled));

    private static int CountKind(IReadOnlyList<TaskItem> tasks, SystemColumnKind kind)
        => tasks.Count(task => task.Column.SystemKind == kind);

    private static int CountCustom(IReadOnlyList<TaskItem> tasks)
        => tasks.Count(task => task.Column.SystemKind is null);

    private static IReadOnlyList<DashboardTaskDto> BuildTaskList(IReadOnlyList<TaskItem> assigned)
        => assigned
            .Where(task => task.Column.SystemKind is null or SystemColumnKind.ToDo)
            .Select(task => task.ToDashboardTaskDto())
            .ToList();

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
