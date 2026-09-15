using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Areas;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/areas")]
[Authorize]
public class AreasController : ControllerBase
{
    private readonly BoardService boardService;

    public AreasController(BoardService boardService)
    {
        this.boardService = boardService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AreaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AreaResponseDto>> Create(
        [FromBody] CreateAreaRequestDto request,
        CancellationToken cancellationToken)
    {
        var area = await boardService.CreateAreaAsync(
            request, User.GetAuthenticatedUserId(), cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { }, area);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AreaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<AreaResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await boardService.GetAreasAsync(cancellationToken));
    }
}
