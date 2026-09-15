using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Users;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await authService.GetUsersAsync(cancellationToken));
    }
}
