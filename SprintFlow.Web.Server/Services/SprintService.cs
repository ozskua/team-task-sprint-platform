using SprintFlow.Web.Server.Common;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.Data.Repositories;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

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

        if (request.EndDate <= request.StartDate)
        {
            return (CreateSprintOutcome.EndNotAfterStart, null);
        }

        var (outcome, sprintId) = await boardRepository.RunBoardOrderingAsync(
            boardId,
            async token =>
            {
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

                    CreatedByUserId = createdByUserId
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

        var taskCounts = await taskRepository.CountByBoardSprintAsync(boardId, cancellationToken);

        var today = clock.Today();

        return sprints
            .Select(sprint => sprint.ToResponseDto(
                StatusOf(sprint, today),
                taskCounts.GetValueOrDefault(sprint.SprintId)))
            .ToList();
    }

    public async Task<IReadOnlyList<SprintListItemResponseDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var sprints = await sprintRepository.GetAllWithBoardAsync(cancellationToken);

        var taskCounts = await taskRepository.CountBySprintAsync(cancellationToken);

        var today = clock.Today();

        return sprints
            .Select(sprint => sprint.ToListItemResponseDto(
                StatusOf(sprint, today),
                taskCounts.GetValueOrDefault(sprint.SprintId)))
            .ToList();
    }

    public async Task<IReadOnlyList<SprintReportRowDto>> GetReportAsync(
        CancellationToken cancellationToken)
    {
        var sprints = await sprintRepository.GetAllWithBoardAsync(cancellationToken);

        var counts = await taskRepository.CountBySprintAndColumnKindAsync(cancellationToken);

        var bySprint = counts
            .GroupBy(row => row.SprintId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<SprintColumnKindCount>)[.. group]);

        var today = clock.Today();

        return sprints
            .OrderByDescending(sprint => sprint.StartDate)
            .ThenByDescending(sprint => sprint.SprintId)
            .Select(sprint => sprint.ToReportRowDto(
                StatusOf(sprint, today),
                OutcomeOf(bySprint.GetValueOrDefault(sprint.SprintId, []))))
            .ToList();
    }

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

        return sprint.ToResponseDto(
            SprintStatus.Active,
            await taskRepository.CountInSprintAsync(sprint.SprintId, cancellationToken));
    }

    private static SprintStatus StatusOf(Sprint sprint, DateOnly today)
    {
        if (today < sprint.StartDate)
        {
            return SprintStatus.Upcoming;
        }

        return today < sprint.EndDate ? SprintStatus.Active : SprintStatus.Archived;
    }

    private async Task<SprintResponseDto> ReadBackAsync(int sprintId, CancellationToken cancellationToken)
    {
        var saved = await sprintRepository.GetDetailAsync(sprintId, cancellationToken)
            ?? throw new InvalidOperationException($"Sprint {sprintId} was saved but could not be read back.");

        return saved.ToResponseDto(StatusOf(saved, clock.Today()), 0);
    }
}
