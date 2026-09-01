using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Invitations;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// Registration invitations: the only way an account can come into existence.
/// </summary>
/// <remarks>
/// Creating one requires nothing but being signed in. Inviting is not an administrative act — a
/// Member may bring someone in exactly as an Admin may — so there is no role requirement here and
/// no permission system behind it.
/// </remarks>
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

    /// <summary>
    /// Creates an invitation for the signed-in user to share. The token comes back once and is not
    /// recoverable afterwards — only its digest is stored.
    /// </summary>
    /// <remarks>
    /// No request body: everything about the invitation is the server's to decide. The inviter comes
    /// from the caller's token, and the expiry from configuration.
    /// </remarks>
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

    /// <summary>
    /// Whether an invitation link still works, so the registration page can show the form or say
    /// why it cannot.
    /// </summary>
    /// <remarks>
    /// Anonymous of necessity: the person holding the link has no account yet — that is the point of
    /// having one. It reveals only whether a token is usable, and a token is 256 bits of randomness,
    /// so there is nothing here to probe for.
    /// </remarks>
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

    /// <summary>
    /// Turns an unusable token into the response that explains it. Shared with
    /// <see cref="AuthController"/> so the registration page gets the same wording whether it asked
    /// before submitting or found out on submit.
    /// </summary>
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

            // NotFound included: a token nobody issued and a token that never existed are the same
            // thing to the person holding it, and both are answered here rather than as a 404 —
            // nothing was looked up by id, so there is no resource to be missing.
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
