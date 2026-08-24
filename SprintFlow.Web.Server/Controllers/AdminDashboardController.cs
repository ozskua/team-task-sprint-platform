using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.AdminDashboard;
using SprintFlow.Web.Server.Interfaces.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// The system-wide overview behind the Admin dashboard. Read-only.
/// </summary>
/// <remarks>
/// Authentication only, like every other endpoint here — no role requirement, because introducing
/// one would be a change to the authorization model rather than a change to this page. A Member who
/// called this directly would be served, which is recorded as known debt: it exposes no data they
/// cannot already read through the board endpoints, only a convenient rollup of it.
/// </remarks>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService adminDashboardService;

    public AdminDashboardController(IAdminDashboardService adminDashboardService)
    {
        this.adminDashboardService = adminDashboardService;
    }

    /// <summary>The system's task counts, user and board totals, and every task in it.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AdminDashboardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminDashboardResponseDto>> Get(CancellationToken cancellationToken)
    {
        // An empty system is not a missing resource: it answers with zeros and an empty list, so
        // there is no 404 to return here.
        return Ok(await adminDashboardService.GetAsync(cancellationToken));
    }
}
