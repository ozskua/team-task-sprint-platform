using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Options;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/admin/sprint-report")]
[Authorize]
public class AdminSprintReportController : ControllerBase
{
    private readonly SprintService sprintService;
    private readonly AuthOptions authOptions;

    public AdminSprintReportController(SprintService sprintService, IOptions<AuthOptions> authOptions)
    {
        this.sprintService = sprintService;
        this.authOptions = authOptions.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SprintReportRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SprintReportRowDto>>> Get(
        CancellationToken cancellationToken)
    {
        if (User.GetAuthenticatedUserId() != authOptions.AdminUserId)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "This report is available to the administrator only.");
        }

        return Ok(await sprintService.GetReportAsync(cancellationToken));
    }
}
