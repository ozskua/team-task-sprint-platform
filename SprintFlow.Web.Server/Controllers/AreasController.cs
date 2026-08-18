using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Areas;
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
        var area = await areaService.CreateAsync(request, GetAuthenticatedUserId(), cancellationToken);

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

    /// <summary>
    /// The creator, taken from the validated token. The request body has no say in this — a client
    /// that sends its own id is simply not modelled by <see cref="CreateAreaRequestDto"/>.
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
