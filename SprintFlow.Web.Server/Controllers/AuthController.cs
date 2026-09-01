using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Auth;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService authService;
    private readonly InvitationService invitationService;

    public AuthController(AuthService authService, InvitationService invitationService)
    {
        this.authService = authService;
        this.invitationService = invitationService;
    }

    /// <summary>Signs a user in. Returns 401 with a generic message for any failure.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        // No business rule here on purpose — the controller routes and returns.
        return Ok(await authService.LoginAsync(request, cancellationToken));
    }

    /// <summary>
    /// Creates an account from an invitation. Anonymous, but not open: without a usable token
    /// nothing is created, so accounts still exist only because someone already signed in decided
    /// one should.
    /// </summary>
    /// <remarks>
    /// No token is issued here. The new account signs in through <c>login</c> like every other one,
    /// so there is exactly one way to obtain a JWT and exactly one place a session begins.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponseDto>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var (outcome, tokenStatus, user) = await invitationService.RegisterAsync(request, cancellationToken);

        return outcome switch
        {
            RegisterOutcome.Created => StatusCode(StatusCodes.Status201Created, user),

            // Reported in the invitation's own words, identical to what the pre-check returns, so
            // the page says the same thing whenever it finds out.
            RegisterOutcome.InvitationNotUsable =>
                InvitationsController.InvitationProblem(this, tokenStatus),

            RegisterOutcome.EmailAlreadyRegistered => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "E-mail already registered",
                detail: "An account already uses this e-mail address. Sign in instead, or register with a different address."),

            // A limit of the hashing algorithm rather than a password rule: bcrypt reads no more
            // than 72 bytes, so a longer password would be stored as something other than what was
            // typed. Said in bytes because that is what is actually measured.
            RegisterOutcome.PasswordTooLong => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Password too long",
                detail: "Password must be at most 72 bytes. Accented and Turkish characters count as two bytes each."),

            _ => throw new InvalidOperationException($"Unhandled register outcome {outcome}.")
        };
    }
}
