using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.AdminDashboard;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
public class AdminDashboardController : ControllerBase
{
    private readonly DashboardService dashboardService;

    public AdminDashboardController(DashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminDashboardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminDashboardResponseDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetSystemAsync(cancellationToken));
    }
}
