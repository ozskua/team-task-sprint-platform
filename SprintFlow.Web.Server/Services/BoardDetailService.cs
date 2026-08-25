using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Assembles the board page. Separate from <see cref="BoardService"/>, which creates boards: the
/// two share no logic and putting them together would only mean one class doing two jobs.
/// </summary>
/// <remarks>
/// One board, one set of columns, one kanban. What changes between callers is only which of the
/// board's tasks are put into those columns:
/// <list type="bullet">
///   <item>the backlog asks for all of them;</item>
///   <item>the kanban asks for one sprint's, either the one it named or the one running today;</item>
///   <item>a board with no sprint running has an empty kanban, not a full one.</item>
/// </list>
/// </remarks>
public class BoardDetailService : IBoardDetailService
{
    private readonly IBoardRepository boardRepository;
    private readonly ITaskParticipantRepository participantRepository;
    private readonly ISprintService sprintService;

    public BoardDetailService(
        IBoardRepository boardRepository,
        ITaskParticipantRepository participantRepository,
        ISprintService sprintService)
    {
        this.boardRepository = boardRepository;
        this.participantRepository = participantRepository;
        this.sprintService = sprintService;
    }

    public async Task<(GetBoardDetailOutcome Outcome, BoardDetailResponseDto? Board)> GetAsync(
        int boardId,
        int? sprintId,
        bool allTasks,
        CancellationToken cancellationToken)
    {
        // The two say opposite things about what to show. Refused rather than letting one win
        // quietly, because either choice would be wrong for the caller that sent the other.
        if (allTasks && sprintId is not null)
        {
            return (GetBoardDetailOutcome.ContradictoryScope, null);
        }

        SprintResponseDto? selectedSprint = null;

        if (!allTasks)
        {
            if (sprintId is int requested)
            {
                // Resolved before the board is read, so a sprint this board does not have is
                // reported as a bad request rather than silently producing an empty board.
                selectedSprint = await sprintService.GetForBoardAsync(boardId, requested, cancellationToken);

                if (selectedSprint is null)
                {
                    // Covers both "no such sprint" and "belongs to another board" — a caller who
                    // named a sprint that is not this board's got the request wrong either way.
                    return (GetBoardDetailOutcome.SprintNotOnBoard, null);
                }
            }
            else
            {
                // Nothing named, so the kanban shows the work actually in progress. Null here is a
                // normal state — no sprints at all, or today falls between two of them — and it
                // leaves the board with its columns and no cards.
                selectedSprint = await sprintService.GetActiveForBoardAsync(boardId, cancellationToken);
            }
        }

        var board = await boardRepository.GetDetailAsync(
            boardId,
            selectedSprint?.SprintId,
            allTasks,
            cancellationToken);

        if (board is null)
        {
            return (GetBoardDetailOutcome.BoardNotFound, null);
        }

        // A second query rather than part of the graph above, because membership is not a
        // relationship the board has — it is worked out from who participates in its tasks.
        //
        // Deliberately not filtered by the selected sprint: membership is about the board, and
        // narrowing it would make people seem to leave the team whenever a sprint is chosen.
        var members = await participantRepository.GetBoardMembersAsync(boardId, cancellationToken);

        return (
            GetBoardDetailOutcome.Found,
            board.ToDetailResponseDto(members, selectedSprint));
    }
}
