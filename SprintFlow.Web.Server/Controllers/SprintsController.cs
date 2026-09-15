using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/sprints")]
[Authorize]
public class SprintsController : ControllerBase
{
    private readonly SprintService sprintService;

    public SprintsController(SprintService sprintService)
    {
        this.sprintService = sprintService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SprintListItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<SprintListItemResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await sprintService.GetAllAsync(cancellationToken));
    }

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
