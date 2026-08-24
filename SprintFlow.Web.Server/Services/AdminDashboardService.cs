using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.AdminDashboard;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Assembles the admin overview. Like the other dashboard service it owns no repository of its own —
/// it reads through three existing ones and decides what the answer means.
/// </summary>
/// <remarks>
/// Three round trips, whatever the size of the system: one query for the tasks, and two scalar
/// counts. The four buckets are worked out from the task rows already in hand rather than by asking
/// the database to aggregate a second time, and no user row is ever loaded — the dashboard needs the
/// number of accounts, not the directory, which the Users page already serves.
/// </remarks>
public class AdminDashboardService : IAdminDashboardService
{
    private readonly ITaskRepository taskRepository;
    private readonly IUserRepository userRepository;
    private readonly IBoardRepository boardRepository;

    public AdminDashboardService(
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        IBoardRepository boardRepository)
    {
        this.taskRepository = taskRepository;
        this.userRepository = userRepository;
        this.boardRepository = boardRepository;
    }

    public async Task<AdminDashboardResponseDto> GetAsync(CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.GetAllWithDetailsAsync(cancellationToken);
        var totalUsers = await userRepository.CountAsync(cancellationToken);
        var totalBoards = await boardRepository.CountAsync(cancellationToken);

        return new AdminDashboardResponseDto(
            CountByBucket(tasks, totalUsers, totalBoards),
            tasks.Select(task => task.ToAdminDashboardTaskDto()).ToList());
    }

    /// <summary>
    /// The six figures. Every column a board can have falls into exactly one of the four task
    /// buckets, so those four always add up to the total number of tasks.
    /// </summary>
    private static AdminDashboardCountsDto CountByBucket(
        IReadOnlyList<TaskItem> tasks,
        int totalUsers,
        int totalBoards)
        => new(
            tasks.Count(task => task.Column.SystemKind == SystemColumnKind.ToDo),
            // A custom column is one with no system kind — Review, Testing, Bench, anything the
            // board's own people added.
            tasks.Count(task => task.Column.SystemKind is null),
            tasks.Count(task => task.Column.SystemKind == SystemColumnKind.Completed),
            tasks.Count(task => task.Column.SystemKind == SystemColumnKind.Cancelled),
            totalUsers,
            totalBoards);
}
