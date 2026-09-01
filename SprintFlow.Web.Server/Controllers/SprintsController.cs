using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// One sprint, addressed by its own id. Creating and listing sprints happen under the board they
/// belong to and live in <see cref="BoardDetailController"/>, the same split the workflow columns
/// already use.
/// </summary>
[ApiController]
[Route("api/sprints")]
// Authentication only, like everywhere else.
[Authorize]
public class SprintsController : ControllerBase
{
    private readonly SprintService sprintService;

    public SprintsController(SprintService sprintService)
    {
        this.sprintService = sprintService;
    }

    /// <summary>
    /// Every sprint in the system, for the overview page. Read-only — sprints are created under the
    /// board they belong to, not here.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SprintListItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<SprintListItemResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        // A system with no sprints is not a missing resource: it answers with an empty list.
        return Ok(await sprintService.GetAllAsync(cancellationToken));
    }

    /// <summary>
    /// The tasks in one sprint, for the sprint's own page and the system-wide sprint list.
    /// </summary>
    /// <remarks>
    /// Authentication only, exactly like every other endpoint in this application. There is no
    /// board- or sprint-scoped access control to apply: the project has no policies, no handlers and
    /// no membership permission, and ownership is recorded without granting anything (D6). Any
    /// signed-in user can already read every task on every board through the board endpoints, so
    /// this exposes nothing new — it is the same known gap D11 records, not a new one.
    /// </remarks>
    [HttpGet("{sprintId:int}/tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<SprintTaskResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SprintTaskResponseDto>>> GetTasks(
        int sprintId,
        CancellationToken cancellationToken)
    {
        var tasks = await sprintService.GetTasksAsync(sprintId, cancellationToken);

        return tasks is null ? SprintNotFound() : Ok(tasks);
    }

    /// <summary>The sprint's own page: what it is called, which days it covers, and how it stands.</summary>
    [HttpGet("{sprintId:int}")]
    [ProducesResponseType(typeof(SprintDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SprintDetailResponseDto>> Get(
        int sprintId,
        CancellationToken cancellationToken)
    {
        var sprint = await sprintService.GetAsync(sprintId, cancellationToken);

        return sprint is null ? SprintNotFound() : Ok(sprint);
    }

    private ObjectResult SprintNotFound()
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Sprint not found",
            detail: "No sprint exists with the requested id.");
}
