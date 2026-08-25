using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// The figures behind one board's Özet tab.
/// </summary>
/// <remarks>
/// Every number is scoped to the board in the route — the task query filters on it and the sprints
/// come from that board's own list, so nothing from another board can reach these totals.
/// <para>
/// Sprint statuses are not derived here. They come from <see cref="ISprintService"/>, which is the
/// one place that decides what "today" means; working it out a second time would let this page and
/// the sprint pages disagree about which sprint is running.
/// </para>
/// </remarks>
public class BoardSummaryService : IBoardSummaryService
{
    private readonly IBoardRepository boardRepository;
    private readonly ITaskRepository taskRepository;
    private readonly ISprintService sprintService;

    public BoardSummaryService(
        IBoardRepository boardRepository,
        ITaskRepository taskRepository,
        ISprintService sprintService)
    {
        this.boardRepository = boardRepository;
        this.taskRepository = taskRepository;
        this.sprintService = sprintService;
    }

    public async Task<BoardSummaryResponseDto?> GetAsync(
        int boardId,
        CancellationToken cancellationToken)
    {
        var board = await boardRepository.GetWithAreaAsync(boardId, cancellationToken);

        if (board is null)
        {
            return null;
        }

        var tasks = await taskRepository.GetBoardTasksForSummaryAsync(boardId, cancellationToken);

        // Already carries each sprint's status, decided against the application's timezone.
        var sprints = await sprintService.GetBoardSprintsAsync(boardId, cancellationToken) ?? [];

        var toDo = Count(tasks, SystemColumnKind.ToDo);
        // A custom column is one with no system kind — Review, Testing, anything the board added.
        var open = tasks.Count(task => task.Column.SystemKind is null);
        var completed = Count(tasks, SystemColumnKind.Completed);
        var cancelled = Count(tasks, SystemColumnKind.Cancelled);

        return new BoardSummaryResponseDto(
            board.BoardId,
            board.Name,
            board.Area.Name,
            // The four buckets are exhaustive and mutually exclusive, so this is their sum — but it
            // is taken from the rows themselves rather than by adding them, so a column that somehow
            // fell into no bucket would show up as a mismatch instead of being hidden.
            tasks.Count,
            toDo,
            open,
            completed,
            cancelled,
            ProgressPercent(toDo, open, completed),
            tasks.Count(task => task.SprintId is null),
            CountStatus(sprints, SprintStatus.Active),
            CountStatus(sprints, SprintStatus.Upcoming),
            CountStatus(sprints, SprintStatus.Archived));
    }

    private static int Count(IReadOnlyList<TaskItem> tasks, SystemColumnKind kind)
        => tasks.Count(task => task.Column.SystemKind == kind);

    /// <summary>
    /// Completed over everything that was ever going to be done. Cancelled is left out of the
    /// denominator deliberately: abandoning work is not progress, and counting it would let the
    /// figure be raised by cancelling tasks. The same rule the Dashboard applies system-wide.
    /// </summary>
    private static int ProgressPercent(int toDo, int open, int completed)
    {
        var total = toDo + open + completed;

        return total == 0 ? 0 : (int)Math.Round(completed / (double)total * 100);
    }

    /// <summary>
    /// Statuses arrive as strings because that is how they leave the server everywhere; comparing
    /// against the enum's own name keeps the two from drifting apart silently.
    /// </summary>
    private static int CountStatus(
        IReadOnlyList<DTOs.Sprints.SprintResponseDto> sprints,
        SprintStatus status)
        => sprints.Count(sprint => sprint.Status == status.ToString());
}
