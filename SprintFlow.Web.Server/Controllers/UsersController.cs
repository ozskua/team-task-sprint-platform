using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Users;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// Read-only for now. Creating, editing and disabling users belong to a later phase.
/// </summary>
/// <remarks>
/// Authentication only. Everyone in the company sees the same directory: it is also what the
/// assignee picker reads, and a Member who cannot look someone up cannot assign a task to them.
/// The rows carry no password, hash or token — see <see cref="UserListItemDto"/>.
/// </remarks>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AuthService authService;

    public UsersController(AuthService authService)
    {
        this.authService = authService;
    }

    /// <summary>Lists every user in the company.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await authService.GetUsersAsync(cancellationToken));
    }
}
