using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Teams;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly BoardService boardService;

    public TeamsController(BoardService boardService)
    {
        this.boardService = boardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TeamResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TeamResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await boardService.GetTeamsAsync(cancellationToken));
    }
}
