using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Options;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// Every sprint's outcome, for the administrator. Read-only.
/// </summary>
/// <remarks>
/// The first endpoint in this project that is not open to every signed-in user. Admin is decided the
/// way <c>AuthService</c> and the Angular guard decide it — the caller's id against
/// <c>Auth:AdminUserId</c> — so there is one definition of admin rather than two. Deliberately
/// <b>not</b> <c>[Authorize(Roles = ...)]</c>: role names are a second definition, and
/// <c>docs/AUTHENTICATION.md</c> §3 fixes the decision on the id. No policy, handler or role system
/// was introduced; this is one condition in one action, like the author check on deleting a comment.
/// <para>
/// <c>/api/admin/dashboard</c> is deliberately left as it was — see the roadmap's D11.
/// </para>
/// </remarks>
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

    /// <summary>Every sprint with its completed, cancelled and still-open counts and percentages.</summary>
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

        // A system with no sprints is not a missing resource: it answers with an empty list.
        return Ok(await sprintService.GetReportAsync(cancellationToken));
    }
}
