using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Areas;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Interfaces.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// Areas are created by any signed-in user. Editing, deleting and membership do not exist yet.
/// </summary>
[ApiController]
[Route("api/areas")]
// Authentication only. Deliberately NOT [Authorize(Roles = "Admin")]: every authenticated user may
// create an Area, and doing so grants them nothing beyond being recorded as the creator.
[Authorize]
public class AreasController : ControllerBase
{
    private readonly IAreaService areaService;

    public AreasController(IAreaService areaService)
    {
        this.areaService = areaService;
    }

    /// <summary>Creates an Area owned by the caller.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AreaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AreaResponseDto>> Create(
        [FromBody] CreateAreaRequestDto request,
        CancellationToken cancellationToken)
    {
        var area = await areaService.CreateAsync(
            request, User.GetAuthenticatedUserId(), cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { }, area);
    }

    /// <summary>Lists every Area. There is no membership to filter by.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AreaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<AreaResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await areaService.GetAllAsync(cancellationToken));
    }
}
