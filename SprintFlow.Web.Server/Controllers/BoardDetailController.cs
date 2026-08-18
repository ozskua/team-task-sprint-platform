using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.DTOs.Columns;
using SprintFlow.Web.Server.DTOs.Tasks;
using SprintFlow.Web.Server.Interfaces.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// One board and the things created inside it. Separate from <see cref="BoardsController"/>, whose
/// route is nested under an Area because that is where a board is created; a board's own page is
/// not addressed that way.
/// </summary>
[ApiController]
[Route("api/boards")]
// Authentication only, like everywhere else. Being the board's creator, or a member of its team,
// is not required for anything here — and requiring membership to create a task would be
// impossible anyway, since membership comes from participating in tasks that do not exist yet.
[Authorize]
public class BoardDetailController : ControllerBase
{
    private readonly IBoardDetailService boardDetailService;
    private readonly IWorkflowColumnService workflowColumnService;
    private readonly ITaskService taskService;
    private readonly IBoardService boardService;

    public BoardDetailController(
        IBoardDetailService boardDetailService,
        IWorkflowColumnService workflowColumnService,
        ITaskService taskService,
        IBoardService boardService)
    {
        this.boardDetailService = boardDetailService;
        this.workflowColumnService = workflowColumnService;
        this.taskService = taskService;
        this.boardService = boardService;
    }

    /// <summary>The board page: metadata, creator, computed team members, columns and tasks.</summary>
    [HttpGet("{boardId:int}")]
    [ProducesResponseType(typeof(BoardDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardDetailResponseDto>> Get(
        int boardId,
        CancellationToken cancellationToken)
    {
        var board = await boardDetailService.GetAsync(boardId, cancellationToken);

        return board is null ? BoardNotFound() : Ok(board);
    }

    /// <summary>Adds a custom column, which always lands immediately before Completed.</summary>
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

    /// <summary>
    /// Reorders the board's custom columns. To Do stays first and Completed and Cancelled stay
    /// last whatever is sent, so naming one of them is rejected rather than quietly ignored.
    /// </summary>
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

    /// <summary>
    /// Creates a task. Without a column id it starts in the board's To Do column; with one, that
    /// column must belong to this board.
    /// </summary>
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
            GetAuthenticatedUserId(),
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

            _ => throw new InvalidOperationException($"Unhandled create-task outcome {outcome}.")
        };
    }

    /// <summary>Renames the board. Nothing else about it is editable.</summary>
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

    /// <summary>
    /// Deletes the board outright. Its team, columns, tasks and assignments go with it by cascade —
    /// the schema already describes that, so nothing is removed row by row here.
    /// </summary>
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

    /// <summary>
    /// The creator, taken from the validated token. The request body has no say in this — a client
    /// that sends its own id is simply not modelled by <see cref="CreateTaskRequestDto"/>.
    /// </summary>
    private int GetAuthenticatedUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Every token this server issues carries the claim, so its absence means the server is
        // misconfigured — not that the caller did anything wrong. A 500 states that honestly.
        if (!int.TryParse(claim, out var userId))
        {
            throw new InvalidOperationException(
                "The authenticated token carries no usable NameIdentifier claim.");
        }

        return userId;
    }
}
