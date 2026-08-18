using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Teams;
using SprintFlow.Web.Server.Interfaces.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// Read-only. Teams are created with their board and never by hand, they have no name of their own
/// to edit, and their membership follows from task assignment — so there is nothing here but a list.
/// </summary>
[ApiController]
[Route("api/teams")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly ITeamService teamService;

    public TeamsController(ITeamService teamService)
    {
        this.teamService = teamService;
    }

    /// <summary>Every team, with the members currently derived from assignment.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TeamResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TeamResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await teamService.GetAllAsync(cancellationToken));
    }
}
