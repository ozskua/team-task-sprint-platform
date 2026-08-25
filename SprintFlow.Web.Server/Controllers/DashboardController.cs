using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Dashboard;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Interfaces.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// The signed-in user's own dashboard. Read-only, and always about the caller.
/// </summary>
/// <remarks>
/// There is no user id in the route, the query string or a body, so this endpoint cannot be asked
/// for anyone else's tasks — not by a tampered request, because there is nothing to tamper with.
/// The caller comes from the validated token and nowhere else.
/// </remarks>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    /// <summary>The caller's assigned tasks, their teams, and the counts across both.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardResponseDto>> Get(CancellationToken cancellationToken)
    {
        // A user with nothing assigned is not a missing resource: they get zeros and empty lists,
        // so there is no 404 to return here.
        return Ok(await dashboardService.GetForUserAsync(
            User.GetAuthenticatedUserId(),
            cancellationToken));
    }
}
