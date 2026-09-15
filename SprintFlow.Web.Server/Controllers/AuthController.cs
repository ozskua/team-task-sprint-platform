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

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await authService.LoginAsync(request, cancellationToken));
    }

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

            RegisterOutcome.InvitationNotUsable =>
                InvitationsController.InvitationProblem(this, tokenStatus),

            RegisterOutcome.EmailAlreadyRegistered => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "E-mail already registered",
                detail: "An account already uses this e-mail address. Sign in instead, or register with a different address."),

            RegisterOutcome.PasswordTooLong => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Password too long",
                detail: "Password must be at most 72 bytes. Accented and Turkish characters count as two bytes each."),

            _ => throw new InvalidOperationException($"Unhandled register outcome {outcome}.")
        };
    }
}
