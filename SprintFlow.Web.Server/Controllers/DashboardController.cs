using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Dashboard;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardResponseDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetForUserAsync(
            User.GetAuthenticatedUserId(),
            cancellationToken));
    }
}
