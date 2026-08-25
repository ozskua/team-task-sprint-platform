using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Sprints, and the one rule that governs them: a board's sprints form a single timeline, so no two
/// of them may cover the same day.
/// </summary>
/// <remarks>
/// That rule cannot be a database constraint. A unique index compares values for equality, and two
/// overlapping intervals are not equal — <c>[01 Sep, 15 Sep)</c> and <c>[10 Sep, 20 Sep)</c> are
/// different pairs. A CHECK constraint cannot help either, because it only sees the row being
/// written and the rule is about the other rows. SQL Server has no interval-exclusion constraint.
/// <para>
/// So it is enforced the way the project already enforces every other rule that depends on reading
/// rows before writing one: inside <c>RunBoardOrderingAsync</c>, which holds an exclusive lock on
/// the board's row. Two requests creating overlapping sprints on one board are serialised by it, and
/// the second one's check sees the first one's committed row. The scope of the rule is the board,
/// and the board's row is the lock, so the two match exactly.
/// </para>
/// </remarks>
public class SprintService : ISprintService
{
    private readonly ISprintRepository sprintRepository;
    private readonly IBoardRepository boardRepository;
    private readonly ITaskRepository taskRepository;
    private readonly IApplicationClock clock;

    public SprintService(
        ISprintRepository sprintRepository,
        IBoardRepository boardRepository,
        ITaskRepository taskRepository,
        IApplicationClock clock)
    {
        this.sprintRepository = sprintRepository;
        this.boardRepository = boardRepository;
        this.taskRepository = taskRepository;
        this.clock = clock;
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

        // Checked before the lock is taken: it reads nothing but the request, so holding the board
        // to decide it would make every other request on that board wait for no reason. The database
        // refuses it too — CK_Sprints_EndAfterStart — so this check is what makes it a clear 400
        // rather than the constraint being the thing that reports it.
        if (request.EndDate <= request.StartDate)
        {
            return (CreateSprintOutcome.EndNotAfterStart, null);
        }

        var (outcome, sprintId) = await boardRepository.RunBoardOrderingAsync(
            boardId,
            async token =>
            {
                // Read inside the lock. Asked before it was held, the answer could already be stale
                // by the time the insert ran — which is the whole failure this guards against.
                if (await sprintRepository.OverlapsAsync(
                        boardId, request.StartDate, request.EndDate, token))
                {
                    return (CreateSprintOutcome.OverlapsExistingSprint, (int?)null);
                }

                var sprint = new Sprint
                {
                    BoardId = boardId,

                    // Stored without surrounding whitespace. A name that is only whitespace never
                    // reaches here — [Required] rejects it.
                    Name = request.Name.Trim(),

                    // Whole days, straight through. There is no time component to strip: the
                    // property is a DateOnly and the column is a date.
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,

                    // From the caller's token. The request DTO has no property for it, so nothing a
                    // client sends can influence this.
                    CreatedByUserId = createdByUserId

                    // CreatedDate is intentionally not set; the database default supplies it.
                };

                sprintRepository.Add(sprint);

                await sprintRepository.SaveChangesAsync(token);

                return (CreateSprintOutcome.Created, sprint.SprintId);
            },
            cancellationToken);

        // Read back outside the lock: the row is committed by now, and assembling a response is not
        // work the board needs to be held for.
        return sprintId is null
            ? (outcome, null)
            : (outcome, await ReadBackAsync(sprintId.Value, cancellationToken));
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

        // Read once, so every row in one response is judged against the same day. Asking for it per
        // sprint could straddle midnight and report two sprints inconsistently.
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

        // One grouped query for every sprint's task count rather than one count per sprint, so the
        // page costs two round trips whatever the number of boards.
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

        // Belonging to another board is refused exactly as not existing is: a caller scoping this
        // board has no business naming a sprint from a different one.
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

        // Active by definition — it was selected by containing today — so the status is not
        // re-derived from a second reading of the clock.
        return sprint.ToResponseDto(
            SprintStatus.Active,
            await taskRepository.CountInSprintAsync(sprint.SprintId, cancellationToken));
    }

    /// <summary>
    /// Where a sprint sits relative to today. Worked out on every read rather than stored: a sprint
    /// becomes archived because the calendar moved, and nothing in this project runs on a schedule
    /// to notice that.
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

        // Newly created, so it holds no tasks yet. Counting would be a round trip to be told zero.
        return saved.ToResponseDto(StatusOf(saved, clock.Today()), 0);
    }
}
