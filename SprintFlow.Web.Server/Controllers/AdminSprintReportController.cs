using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Common.Enums;
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
    private readonly SprintReportWebhook webhook;
    private readonly IApplicationClock clock;
    private readonly ILogger<AdminSprintReportController> logger;
    private readonly AuthOptions authOptions;

    public AdminSprintReportController(
        SprintService sprintService,
        SprintReportWebhook webhook,
        IApplicationClock clock,
        ILogger<AdminSprintReportController> logger,
        IOptions<AuthOptions> authOptions)
    {
        this.sprintService = sprintService;
        this.webhook = webhook;
        this.clock = clock;
        this.logger = logger;
        this.authOptions = authOptions.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SprintReportRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SprintReportRowDto>>> Get(
        CancellationToken cancellationToken)
    {
        if (Forbidden() is ActionResult forbidden)
        {
            return forbidden;
        }

        return Ok(await sprintService.GetReportAsync(cancellationToken));
    }

    [HttpGet("export/xlsx")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> ExportXlsx(CancellationToken cancellationToken)
        => ExportAsync(SprintReportFormat.Xlsx, cancellationToken);

    [HttpGet("export/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public Task<IActionResult> ExportPdf(CancellationToken cancellationToken)
        => ExportAsync(SprintReportFormat.Pdf, cancellationToken);

    private async Task<IActionResult> ExportAsync(SprintReportFormat format, CancellationToken cancellationToken)
    {
        if (Forbidden() is ActionResult forbidden)
        {
            return forbidden;
        }

        var userId = User.GetAuthenticatedUserId();

        var export = await sprintService.GetReportExportAsync(cancellationToken);

        var file = SprintReportFile.Render(export, format, clock.Today());

        logger.LogInformation(
            "Sprint report exported as {Format} with {SprintCount} sprint(s) and {TaskCount} task(s) by user {UserId}.",
            format,
            export.Rows.Count,
            export.TotalTasks,
            userId);

        var delivery = await webhook.DeliverAsync(export, format, userId, cancellationToken);

        Response.Headers[SprintReportWebhook.ResultHeader] = delivery.ToString();

        return File(file.Bytes, file.ContentType, file.FileName);
    }

    private ActionResult? Forbidden()
        => User.GetAuthenticatedUserId() == authOptions.AdminUserId
            ? null
            : Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "This report is available to the administrator only.");
}
