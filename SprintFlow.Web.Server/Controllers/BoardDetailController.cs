using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/boards")]
[Authorize]
public class BoardDetailController : ControllerBase
{
    private readonly BoardService boardService;
    private readonly WorkflowColumnService workflowColumnService;
    private readonly TaskService taskService;
    private readonly SprintService sprintService;

    public BoardDetailController(
        BoardService boardService,
        WorkflowColumnService workflowColumnService,
        TaskService taskService,
        SprintService sprintService)
    {
        this.boardService = boardService;
        this.workflowColumnService = workflowColumnService;
        this.taskService = taskService;
        this.sprintService = sprintService;
    }

    [HttpGet("{boardId:int}")]
    [ProducesResponseType(typeof(BoardDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardDetailResponseDto>> Get(
        int boardId,
        [FromQuery] int? sprintId,
        [FromQuery] bool allTasks,
        CancellationToken cancellationToken)
    {
        var (outcome, board) = await boardService.GetDetailAsync(
            boardId, sprintId, allTasks, cancellationToken);

        return outcome switch
        {
            GetBoardDetailOutcome.Found => Ok(board),

            GetBoardDetailOutcome.BoardNotFound => BoardNotFound(),

            GetBoardDetailOutcome.SprintNotOnBoard => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Sprint not on this board",
                detail: "No sprint with the requested id belongs to this board."),

            GetBoardDetailOutcome.ContradictoryScope => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Conflicting task scope",
                detail: "Ask for one sprint's tasks or for all of them, not both at once."),

            _ => throw new InvalidOperationException($"Unhandled board detail outcome {outcome}.")
        };
    }

    [HttpPost("{boardId:int}/columns")]
    [ProducesResponseType(typeof(WorkflowColumnResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowColumnResponseDto>> CreateColumn(
        int boardId,
        [FromBody] CreateWorkflowColumnRequestDto request,
        CancellationToken cancellationToken)
    {
        var column = await workflowColumnService.CreateAsync(boardId, request, cancellationToken);

        return column is null
            ? BoardNotFound()
            : StatusCode(StatusCodes.Status201Created, column);
    }

    [HttpPut("{boardId:int}/columns/order")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkflowColumnResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<WorkflowColumnResponseDto>>> ReorderColumns(
        int boardId,
        [FromBody] ReorderColumnsRequestDto request,
        CancellationToken cancellationToken)
    {
        var (outcome, columns) = await workflowColumnService.ReorderAsync(boardId, request, cancellationToken);

        return outcome switch
        {
            ReorderColumnsOutcome.Reordered => Ok(columns),

            ReorderColumnsOutcome.BoardNotFound => BoardNotFound(),

            ReorderColumnsOutcome.ContainsSystemColumn => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "System columns cannot be reordered",
                detail: "To Do stays first, and Completed and Cancelled stay last. Send only the board's custom columns."),

            ReorderColumnsOutcome.ColumnSetMismatch => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Column list does not match the board",
                detail: "The list must contain every custom column of this board exactly once, and nothing else."),

            _ => throw new InvalidOperationException($"Unhandled reorder outcome {outcome}.")
        };
    }

    [HttpPost("{boardId:int}/tasks")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponseDto>> CreateTask(
        int boardId,
        [FromBody] CreateTaskRequestDto request,
        CancellationToken cancellationToken)
    {
        var (outcome, task) = await taskService.CreateAsync(
            boardId,
            request,
            User.GetAuthenticatedUserId(),
            cancellationToken);

        return outcome switch
        {
            CreateTaskOutcome.Created => StatusCode(StatusCodes.Status201Created, task),

            CreateTaskOutcome.BoardNotFound => BoardNotFound(),

            CreateTaskOutcome.ColumnNotFound => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Column not found",
                detail: "No column exists with the requested id."),

            CreateTaskOutcome.ColumnOnDifferentBoard => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Column belongs to a different board",
                detail: "A task can only be created in a column of the board it belongs to. No task was created."),

            CreateTaskOutcome.AssigneeUnavailable => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Assignee unavailable",
                detail: "No active user exists with the requested id. Leave the assignee empty to create the task unassigned."),

            CreateTaskOutcome.SprintNotFound => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Sprint not found",
                detail: "No sprint exists with the requested id. Leave the sprint empty to create the task outside any sprint."),

            CreateTaskOutcome.SprintOnDifferentBoard => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Sprint belongs to a different board",
                detail: "A task can only be created in a sprint of the board it belongs to. No task was created."),

            _ => throw new InvalidOperationException($"Unhandled create-task outcome {outcome}.")
        };
    }

    [HttpGet("{boardId:int}/summary")]
    [ProducesResponseType(typeof(BoardSummaryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardSummaryResponseDto>> GetSummary(
        int boardId,
        CancellationToken cancellationToken)
    {
        var summary = await boardService.GetSummaryAsync(boardId, cancellationToken);

        return summary is null ? BoardNotFound() : Ok(summary);
    }

    [HttpGet("{boardId:int}/sprints")]
    [ProducesResponseType(typeof(IReadOnlyList<SprintResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SprintResponseDto>>> GetSprints(
        int boardId,
        CancellationToken cancellationToken)
    {
        var sprints = await sprintService.GetBoardSprintsAsync(boardId, cancellationToken);

        return sprints is null ? BoardNotFound() : Ok(sprints);
    }

    [HttpPost("{boardId:int}/sprints")]
    [ProducesResponseType(typeof(SprintResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintResponseDto>> CreateSprint(
        int boardId,
        [FromBody] CreateSprintRequestDto request,
        CancellationToken cancellationToken)
    {
        var (outcome, sprint) = await sprintService.CreateAsync(
            boardId,
            request,
            User.GetAuthenticatedUserId(),
            cancellationToken);

        return outcome switch
        {
            CreateSprintOutcome.Created => StatusCode(StatusCodes.Status201Created, sprint),

            CreateSprintOutcome.BoardNotFound => BoardNotFound(),

            CreateSprintOutcome.EndNotAfterStart => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "End date must be after the start date",
                detail: "A sprint runs from its start date up to, but not including, its end date, so the two cannot be the same day."),

            CreateSprintOutcome.OverlapsExistingSprint => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Sprint dates overlap an existing sprint",
                detail: "This board's sprints form one timeline, and another sprint already covers part of these days. No sprint was created."),

            _ => throw new InvalidOperationException($"Unhandled create-sprint outcome {outcome}.")
        };
    }

    [HttpPut("{boardId:int}")]
    [ProducesResponseType(typeof(BoardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardResponseDto>> Rename(
        int boardId,
        [FromBody] CreateBoardRequestDto request,
        CancellationToken cancellationToken)
    {
        var board = await boardService.RenameAsync(boardId, request, cancellationToken);

        return board is null ? BoardNotFound() : Ok(board);
    }

    [HttpDelete("{boardId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int boardId, CancellationToken cancellationToken)
        => await boardService.DeleteAsync(boardId, cancellationToken)
            ? NoContent()
            : BoardNotFound();

    private ObjectResult BoardNotFound()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Board not found",
            detail: "No board exists with the requested id.");
}
