using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Areas;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Options;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/areas")]
[Authorize]
public class AreasController : ControllerBase
{
    private readonly BoardService boardService;
    private readonly AreaWebhookService areaWebhookService;
    private readonly AuthOptions authOptions;

    public AreasController(
        BoardService boardService,
        AreaWebhookService areaWebhookService,
        IOptions<AuthOptions> authOptions)
    {
        this.boardService = boardService;
        this.areaWebhookService = areaWebhookService;
        this.authOptions = authOptions.Value;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AreaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AreaResponseDto>> Create(
        [FromBody] CreateAreaRequestDto request,
        CancellationToken cancellationToken)
    {
        var area = await boardService.CreateAreaAsync(
            request, User.GetAuthenticatedUserId(), cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { }, area);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AreaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<AreaResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await boardService.GetAreasAsync(cancellationToken));
    }

    [HttpGet("{areaId:int}/webhook")]
    [ProducesResponseType(typeof(AreaWebhookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AreaWebhookDto>> GetWebhook(int areaId, CancellationToken cancellationToken)
    {
        if (Forbidden() is ActionResult forbidden)
        {
            return forbidden;
        }

        var target = await areaWebhookService.GetTargetAsync(areaId, cancellationToken);

        if (target is null)
        {
            return NotFound();
        }

        return Ok(new AreaWebhookDto { WebhookUrl = target.WebhookUrl });
    }

    [HttpPut("{areaId:int}/webhook")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetWebhook(
        int areaId,
        [FromBody] AreaWebhookDto request,
        CancellationToken cancellationToken)
    {
        if (Forbidden() is ActionResult forbidden)
        {
            return forbidden;
        }

        var outcome = await areaWebhookService.SetWebhookUrlAsync(areaId, request.WebhookUrl, cancellationToken);

        return outcome switch
        {
            SetAreaWebhookOutcome.Saved => NoContent(),
            SetAreaWebhookOutcome.AreaNotFound => NotFound(),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid webhook URL",
                detail: "Enter an absolute https:// URL of a public host, or leave it empty to disable the webhook.")
        };
    }

    private ActionResult? Forbidden()
        => User.GetAuthenticatedUserId() == authOptions.AdminUserId
            ? null
            : Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Webhook settings are available to the administrator only.");
}
