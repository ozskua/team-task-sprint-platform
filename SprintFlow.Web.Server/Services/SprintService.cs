using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Sprints, and the one rule that governs them: a board's sprints form a single timeline, so no two
/// of them may cover the same day.
/// </summary>
/// <remarks>
/// That rule cannot be a database constraint: a unique index compares values for equality and two
/// overlapping intervals are not equal, while a CHECK constraint only sees the row being written.
/// SQL Server has no interval-exclusion constraint.
/// <para>
/// So it is enforced the way every other read-before-write rule here is: inside
/// <c>RunBoardOrderingAsync</c>, which holds an exclusive lock on the board's row. The scope of the
/// rule is the board and the board's row is the lock, so the two match exactly.
/// </para>
/// </remarks>
public class SprintService
{
    private readonly SprintRepository sprintRepository;
    private readonly BoardRepository boardRepository;
    private readonly TaskRepository taskRepository;
    private readonly IApplicationClock clock;
    private readonly IBoardSummaryCache summaryCache;

    public SprintService(
        SprintRepository sprintRepository,
        BoardRepository boardRepository,
        TaskRepository taskRepository,
        IApplicationClock clock,
        IBoardSummaryCache summaryCache)
    {
        this.sprintRepository = sprintRepository;
        this.boardRepository = boardRepository;
        this.taskRepository = taskRepository;
        this.clock = clock;
        this.summaryCache = summaryCache;
    }

    public async Task<(CreateSprintOutcome Outcome, SprintResponseDto? Sprint)> CreateAsync(
        int boardId,
        CreateSprintRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken)
    {
        if (!await boardRepository.ExistsAsync(boardId, cancellationToken))
        {
            return (CreateSprintOutcome.BoardNotFound, null);
        }

        // Checked before the lock: it reads nothing but the request. The database refuses it too
        // (CK_Sprints_EndAfterStart); this check is what makes it a clear 400.
        if (request.EndDate <= request.StartDate)
        {
            return (CreateSprintOutcome.EndNotAfterStart, null);
        }

        var (outcome, sprintId) = await boardRepository.RunBoardOrderingAsync(
            boardId,
            async token =>
            {
                // Read inside the lock: asked before it was held, the answer could be stale by the
                // time the insert ran.
                if (await sprintRepository.OverlapsAsync(
                        boardId, request.StartDate, request.EndDate, token))
                {
                    return (CreateSprintOutcome.OverlapsExistingSprint, (int?)null);
                }

                var sprint = new Sprint
                {
                    BoardId = boardId,

                    Name = request.Name.Trim(),

                    StartDate = request.StartDate,
                    EndDate = request.EndDate,

                    // From the caller's token; no request DTO property can influence it.
                    CreatedByUserId = createdByUserId

                    // CreatedDate is intentionally not set; the database default supplies it.
                };

                sprintRepository.Add(sprint);

                await sprintRepository.SaveChangesAsync(token);

                return (CreateSprintOutcome.Created, sprint.SprintId);
            },
            cancellationToken);

        if (sprintId is null)
        {
            return (outcome, null);
        }

        await summaryCache.RemoveAsync(boardId, cancellationToken);

        // Read back outside the lock: the row is committed, and assembling a response is not work
        // the board needs to be held for.
        return (outcome, await ReadBackAsync(sprintId.Value, cancellationToken));
    }

    public async Task<IReadOnlyList<SprintResponseDto>?> GetBoardSprintsAsync(
        int boardId,
        CancellationToken cancellationToken)
    {
        if (!await boardRepository.ExistsAsync(boardId, cancellationToken))
        {
            return null;
        }

        var sprints = await sprintRepository.GetBoardSprintsAsync(boardId, cancellationToken);

        // One grouped query for the whole list rather than a count per sprint.
        var taskCounts = await taskRepository.CountByBoardSprintAsync(boardId, cancellationToken);

        // Read once, so every row in one response is judged against the same day — asking per
        // sprint could straddle midnight.
        var today = clock.Today();

        return sprints
            .Select(sprint => sprint.ToResponseDto(
                StatusOf(sprint, today),
                // Absent means none, not missing: a sprint nobody has put a task into yet.
                taskCounts.GetValueOrDefault(sprint.SprintId)))
            .ToList();
    }

    public async Task<IReadOnlyList<SprintListItemResponseDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var sprints = await sprintRepository.GetAllWithBoardAsync(cancellationToken);

        // One grouped query for every sprint's count, so the page costs two round trips whatever
        // the number of boards.
        var taskCounts = await taskRepository.CountBySprintAsync(cancellationToken);

        // Read once, so every row in one response is judged against the same day.
        var today = clock.Today();

        return sprints
            .Select(sprint => sprint.ToListItemResponseDto(
                StatusOf(sprint, today),
                // Absent means none, not missing: a sprint nobody has put a task into yet.
                taskCounts.GetValueOrDefault(sprint.SprintId)))
            .ToList();
    }

    /// <summary>
    /// Every sprint in the system with its outcome: how much was completed, how much was cancelled
    /// and how much is still open. Read by the admin sprint report.
    /// </summary>
    /// <remarks>
    /// Two round trips whatever the number of sprints — the sprints with their board and area, then
    /// one grouped query for the whole breakdown. No task row is loaded.
    /// </remarks>
    public async Task<IReadOnlyList<SprintReportRowDto>> GetReportAsync(
        CancellationToken cancellationToken)
    {
        var sprints = await sprintRepository.GetAllWithBoardAsync(cancellationToken);

        var counts = await taskRepository.CountBySprintAndColumnKindAsync(cancellationToken);

        var bySprint = counts
            .GroupBy(row => row.SprintId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<SprintColumnKindCount>)[.. group]);

        // Read once, so every row in one response is judged against the same day.
        var today = clock.Today();

        return sprints
            // Newest first: the report is read to see how the most recent sprints went. The
            // repository orders by area and board for the sprint list that shares it, so the order
            // is changed here rather than there.
            .OrderByDescending(sprint => sprint.StartDate)
            .ThenByDescending(sprint => sprint.SprintId)
            .Select(sprint => sprint.ToReportRowDto(
                StatusOf(sprint, today),
                // Absent means none, not missing: a sprint nobody has put a task into yet.
                OutcomeOf(bySprint.GetValueOrDefault(sprint.SprintId, []))))
            .ToList();
    }

    /// <summary>
    /// Splits one sprint's counts into completed, cancelled and still open.
    /// </summary>
    /// <remarks>
    /// The column is what says which of the three a task is; the task itself carries no status. This
    /// is the same rule the dashboard and the board summary count by. Ongoing is taken as the
    /// remainder rather than as "To Do plus custom" so the three always add up to the sprint's own
    /// total — a column kind added later is still open work and lands there by construction.
    /// </remarks>
    private static SprintTaskOutcome OutcomeOf(IReadOnlyList<SprintColumnKindCount> rows)
    {
        var total = rows.Sum(row => row.Count);
        var completed = CountKind(rows, SystemColumnKind.Completed);
        var cancelled = CountKind(rows, SystemColumnKind.Cancelled);

        return SprintTaskOutcome.From(completed, cancelled, total - completed - cancelled);
    }

    private static int CountKind(IReadOnlyList<SprintColumnKindCount> rows, SystemColumnKind kind)
        => rows.Where(row => row.SystemKind == kind).Sum(row => row.Count);

    public async Task<SprintDetailResponseDto?> GetAsync(int sprintId, CancellationToken cancellationToken)
    {
        var sprint = await sprintRepository.GetDetailAsync(sprintId, cancellationToken);

        if (sprint is null)
        {
            return null;
        }

        return sprint.ToDetailResponseDto(
            StatusOf(sprint, clock.Today()),
            await taskRepository.CountInSprintAsync(sprintId, cancellationToken));
    }

    public async Task<IReadOnlyList<SprintTaskResponseDto>?> GetTasksAsync(
        int sprintId,
        CancellationToken cancellationToken)
    {
        // Checked first so a sprint that does not exist is a 404 rather than an empty list, which
        // would look like a sprint with nothing in it.
        if (await sprintRepository.GetByIdAsync(sprintId, cancellationToken) is null)
        {
            return null;
        }

        var tasks = await taskRepository.GetBySprintAsync(sprintId, cancellationToken);

        return tasks.Select(task => task.ToSprintTaskResponseDto()).ToList();
    }

    public async Task<SprintResponseDto?> GetForBoardAsync(
        int boardId,
        int sprintId,
        CancellationToken cancellationToken)
    {
        var sprint = await sprintRepository.GetDetailAsync(sprintId, cancellationToken);

        // Belonging to another board is refused exactly as not existing is.
        if (sprint is null || sprint.BoardId != boardId)
        {
            return null;
        }

        return sprint.ToResponseDto(
            StatusOf(sprint, clock.Today()),
            await taskRepository.CountInSprintAsync(sprintId, cancellationToken));
    }

    public async Task<SprintResponseDto?> GetActiveForBoardAsync(
        int boardId,
        CancellationToken cancellationToken)
    {
        var today = clock.Today();

        var sprint = await sprintRepository.GetActiveAsync(boardId, today, cancellationToken);

        if (sprint is null)
        {
            return null;
        }

        // Active by definition: it was selected by containing today.
        return sprint.ToResponseDto(
            SprintStatus.Active,
            await taskRepository.CountInSprintAsync(sprint.SprintId, cancellationToken));
    }

    /// <summary>
    /// Where a sprint sits relative to today. Worked out on every read rather than stored: a sprint
    /// becomes archived because the calendar moved, and nothing here runs on a schedule to notice.
    /// </summary>
    /// <remarks>
    /// The end day is excluded, so a sprint is already archived on its own <c>EndDate</c> — the last
    /// day it was running was the day before.
    /// </remarks>
    private static SprintStatus StatusOf(Sprint sprint, DateOnly today)
    {
        if (today < sprint.StartDate)
        {
            return SprintStatus.Upcoming;
        }

        return today < sprint.EndDate ? SprintStatus.Active : SprintStatus.Archived;
    }

    /// <summary>
    /// Re-reads the sprint so the response carries the creator's name and the timestamp the database
    /// generated, rather than anything assembled here.
    /// </summary>
    private async Task<SprintResponseDto> ReadBackAsync(int sprintId, CancellationToken cancellationToken)
    {
        var saved = await sprintRepository.GetDetailAsync(sprintId, cancellationToken)
            ?? throw new InvalidOperationException($"Sprint {sprintId} was saved but could not be read back.");

        // Newly created, so it holds no tasks: counting would be a round trip to be told zero.
        return saved.ToResponseDto(StatusOf(saved, clock.Today()), 0);
    }
}
