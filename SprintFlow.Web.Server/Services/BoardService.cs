using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.Areas;
using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.DTOs.Teams;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// The board hierarchy: areas, the boards inside them, each board's one team, and the two views of
/// a board — its full page and its summary figures.
/// </summary>
public class BoardService
{
    private readonly BoardRepository boardRepository;
    private readonly TaskRepository taskRepository;
    private readonly SprintService sprintService;
    private readonly IBoardSummaryCache summaryCache;

    public BoardService(
        BoardRepository boardRepository,
        TaskRepository taskRepository,
        SprintService sprintService,
        IBoardSummaryCache summaryCache)
    {
        this.boardRepository = boardRepository;
        this.taskRepository = taskRepository;
        this.sprintService = sprintService;
        this.summaryCache = summaryCache;
    }

    // ---- Areas ----

    public async Task<AreaResponseDto> CreateAreaAsync(
        CreateAreaRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken)
    {
        // An area is created on its own; boards are added afterwards and named by a user.
        var area = new Area
        {
            Name = request.Name.Trim(),
            CreatedByUserId = createdByUserId
        };

        var areaId = await boardRepository.AddAreaAsync(area, cancellationToken);

        // Re-read so the response carries database-generated values rather than anything assembled
        // here.
        var created = await boardRepository.GetAreaAsync(areaId, cancellationToken)
            ?? throw new InvalidOperationException($"Area {areaId} was saved but could not be read back.");

        return created.ToResponseDto();
    }

    public async Task<IReadOnlyList<AreaResponseDto>> GetAreasAsync(CancellationToken cancellationToken)
    {
        var areas = await boardRepository.GetAreasAsync(cancellationToken);

        return areas.Select(area => area.ToResponseDto()).ToList();
    }

    // ---- Boards ----

    public async Task<BoardResponseDto?> CreateAsync(
        int areaId,
        CreateBoardRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken)
    {
        // The only check. Deliberately not "is the caller the area's owner": any authenticated user
        // may add a board to any area, and ownership grants nothing.
        if (!await boardRepository.AreaExistsAsync(areaId, cancellationToken))
        {
            return null;
        }

        var board = new Board
        {
            AreaId = areaId,
            Name = request.Name.Trim(),
            CreatedByUserId = createdByUserId,

            // Every board has exactly one team and three system columns, written in the same save
            // so a board can never exist without them.
            Team = new Team { CreatedByUserId = createdByUserId },
            WorkflowColumns = BuildSystemColumns()
        };

        var boardId = await boardRepository.AddAsync(board, cancellationToken);

        var created = await boardRepository.GetByIdAsync(boardId, cancellationToken)
            ?? throw new InvalidOperationException($"Board {boardId} was saved but could not be read back.");

        return created.ToResponseDto();
    }

    public async Task<BoardResponseDto?> RenameAsync(
        int boardId,
        CreateBoardRequestDto request,
        CancellationToken cancellationToken)
    {
        var board = await boardRepository.GetForUpdateAsync(boardId, cancellationToken);

        if (board is null)
        {
            return null;
        }

        board.Name = request.Name.Trim();

        await boardRepository.SaveChangesAsync(cancellationToken);

        // The summary carries the board's name, so a rename makes the cached copy wrong even though
        // no figure in it changed.
        await summaryCache.RemoveAsync(boardId, cancellationToken);

        var renamed = await boardRepository.GetByIdAsync(boardId, cancellationToken)
            ?? throw new InvalidOperationException($"Board {boardId} was saved but could not be read back.");

        return renamed.ToResponseDto();
    }

    public async Task<bool> DeleteAsync(int boardId, CancellationToken cancellationToken)
    {
        var board = await boardRepository.GetForUpdateAsync(boardId, cancellationToken);

        if (board is null)
        {
            return false;
        }

        boardRepository.Remove(board);

        await boardRepository.SaveChangesAsync(cancellationToken);
        await summaryCache.RemoveAsync(boardId, cancellationToken);

        return true;
    }

    // ---- The board page ----

    /// <summary>
    /// One board with its columns, and whichever of its tasks the caller is scoped to: all of them
    /// (backlog), one sprint's (kanban), or none when no sprint is running.
    /// </summary>
    public async Task<(GetBoardDetailOutcome Outcome, BoardDetailResponseDto? Board)> GetDetailAsync(
        int boardId,
        int? sprintId,
        bool allTasks,
        CancellationToken cancellationToken)
    {
        // The two say opposite things about what to show, so neither is allowed to win quietly.
        if (allTasks && sprintId is not null)
        {
            return (GetBoardDetailOutcome.ContradictoryScope, null);
        }

        SprintResponseDto? selectedSprint = null;

        if (!allTasks)
        {
            if (sprintId is int requested)
            {
                // Resolved before the board is read, so a sprint this board does not have is a bad
                // request rather than a silently empty board.
                selectedSprint = await sprintService.GetForBoardAsync(boardId, requested, cancellationToken);

                if (selectedSprint is null)
                {
                    return (GetBoardDetailOutcome.SprintNotOnBoard, null);
                }
            }
            else
            {
                // Nothing named, so the kanban shows the work in progress. Null is normal — no
                // sprints at all, or today falls between two — and leaves the board with no cards.
                selectedSprint = await sprintService.GetActiveForBoardAsync(boardId, cancellationToken);
            }
        }

        var board = await boardRepository.GetDetailAsync(
            boardId, selectedSprint?.SprintId, allTasks, cancellationToken);

        if (board is null)
        {
            return (GetBoardDetailOutcome.BoardNotFound, null);
        }

        // Membership is computed from task participation rather than stored, and is deliberately
        // not filtered by the selected sprint — narrowing it would make people appear to leave the
        // team whenever a sprint is chosen.
        var members = await taskRepository.GetBoardMembersAsync(boardId, cancellationToken);

        return (GetBoardDetailOutcome.Found, board.ToDetailResponseDto(members, selectedSprint));
    }

    // ---- The board's figures ----

    /// <summary>
    /// One board's Özet figures. Cached per board; the writes that change them drop the entry as
    /// they commit.
    /// </summary>
    public async Task<BoardSummaryResponseDto?> GetSummaryAsync(
        int boardId,
        CancellationToken cancellationToken)
    {
        // A miss — including Redis being unreachable — falls through and computes the figures.
        if (await summaryCache.GetAsync(boardId, cancellationToken) is BoardSummaryResponseDto cached)
        {
            return cached;
        }

        var board = await boardRepository.GetWithAreaAsync(boardId, cancellationToken);

        if (board is null)
        {
            // Not cached: a 404 is one indexed read, and caching it would hold an entry for every
            // id anyone ever mistyped.
            return null;
        }

        var tasks = await taskRepository.GetBoardTasksForSummaryAsync(boardId, cancellationToken); //DB Tarama 

        // Already carries each sprint's status, decided against the application's timezone.
        var sprints = await sprintService.GetBoardSprintsAsync(boardId, cancellationToken) ?? [];

        var toDo = CountKind(tasks, SystemColumnKind.ToDo);
        // A custom column is one with no system kind — Review, Testing, anything the board added.
        var open = tasks.Count(task => task.Column.SystemKind is null);
        var completed = CountKind(tasks, SystemColumnKind.Completed);
        var cancelled = CountKind(tasks, SystemColumnKind.Cancelled);

        var summary = new BoardSummaryResponseDto(
            board.BoardId,
            board.Name,
            board.Area.Name,
            // Taken from the rows rather than by adding the buckets, so a column falling into no
            // bucket shows up as a mismatch instead of being hidden.
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

        await summaryCache.SetAsync(boardId, summary, cancellationToken);

        return summary;
    }

    // ---- Teams ----

    /// <summary>
    /// Every team. Nothing is creatable here: a team exists because a board does, and its members
    /// because tasks are assigned.
    /// </summary>
    public async Task<IReadOnlyList<TeamResponseDto>> GetTeamsAsync(CancellationToken cancellationToken)
    {
        var teams = await boardRepository.GetTeamsAsync(cancellationToken);

        // One query for every board's members rather than one per team.
        var membersByBoard = await taskRepository.GetMembersByBoardAsync(cancellationToken);

        return teams
            .Select(team => team.ToResponseDto(
                membersByBoard.TryGetValue(team.BoardId, out var members) ? members : []))
            .ToList();
    }

    /// <summary>
    /// The three columns every board is required to have, in display order. A fresh list each call,
    /// because EF attaches these instances to the board being created.
    /// </summary>
    /// <remarks>
    /// The same names and positions also appear in the migration that backfilled existing boards.
    /// That duplication is deliberate: a migration records what happened at a point in time and
    /// must not read a constant a later release could change underneath it.
    /// </remarks>
    private static List<WorkflowColumn> BuildSystemColumns()
        =>
        [
            new WorkflowColumn { Name = "To Do", Position = 0, SystemKind = SystemColumnKind.ToDo },
            new WorkflowColumn { Name = "Completed", Position = 1, SystemKind = SystemColumnKind.Completed },
            new WorkflowColumn { Name = "Cancelled", Position = 2, SystemKind = SystemColumnKind.Cancelled }
        ];

    private static int CountKind(IReadOnlyList<TaskItem> tasks, SystemColumnKind kind)
        => tasks.Count(task => task.Column.SystemKind == kind);

    /// <summary>
    /// Completed over everything that was ever going to be done. Cancelled is excluded from the
    /// denominator on purpose: abandoning work is not progress, and counting it would let the
    /// figure be raised by cancelling tasks.
    /// </summary>
    private static int ProgressPercent(int toDo, int open, int completed)
    {
        var total = toDo + open + completed;

        return total == 0 ? 0 : (int)Math.Round(completed / (double)total * 100);
    }

    private static int CountStatus(IReadOnlyList<SprintResponseDto> sprints, SprintStatus status)
        => sprints.Count(sprint => sprint.Status == status.ToString());
}
