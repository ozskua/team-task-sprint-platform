using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Invitations;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/invitations")]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly InvitationService invitationService;

    public InvitationsController(InvitationService invitationService)
    {
        this.invitationService = invitationService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvitationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InvitationResponseDto>> Create(CancellationToken cancellationToken)
    {
        var invitation = await invitationService.CreateAsync(
            User.GetAuthenticatedUserId(),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, invitation);
    }

    [AllowAnonymous]
    [HttpGet("{token}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStatus(string token, CancellationToken cancellationToken)
    {
        var status = await invitationService.GetStatusAsync(token, cancellationToken);

        return status == InvitationTokenStatus.Valid
            ? NoContent()
            : InvitationProblem(status);
    }

    internal static ObjectResult InvitationProblem(ControllerBase controller, InvitationTokenStatus status)
    {
        var (title, detail) = status switch
        {
            InvitationTokenStatus.AlreadyUsed => (
                "Invitation already used",
                "This invitation has already created an account. Ask for a new one."),

            InvitationTokenStatus.Expired => (
                "Invitation expired",
                "This invitation is no longer valid. Ask whoever sent it for a new one."),

            InvitationTokenStatus.InviterInactive => (
                "Invitation no longer valid",
                "The account that created this invitation can no longer sign in. Ask for a new one."),

            _ => (
                "Invitation not valid",
                "This invitation link is not valid. Check it was copied in full, or ask for a new one.")
        };

        return controller.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: title,
            detail: detail);
    }

    private ObjectResult InvitationProblem(InvitationTokenStatus status)
        => InvitationProblem(this, status);
}
