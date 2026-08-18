using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.Interfaces.Services;

namespace SprintFlow.Web.Server.Controllers;

/// <summary>
/// Boards live inside an Area, which is why the route is nested. Renaming and deleting do not
/// exist yet.
/// </summary>
[ApiController]
[Route("api/areas/{areaId:int}/boards")]
// Authentication only. Any signed-in user may add a Board to any Area — being the Area's creator is
// not required, and creating a Board grants no permission over it.
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly IBoardService boardService;

    public BoardsController(IBoardService boardService)
    {
        this.boardService = boardService;
    }

    /// <summary>Creates a Board in the Area, along with the Board's one Team.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BoardResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardResponseDto>> Create(
        int areaId,
        [FromBody] CreateBoardRequestDto request,
        CancellationToken cancellationToken)
    {
        var board = await boardService.CreateAsync(
            areaId,
            request,
            GetAuthenticatedUserId(),
            cancellationToken);

        // Turning "no such Area" into a status code is an HTTP concern, so it belongs here rather
        // than in the service.
        if (board is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Area not found",
                detail: "No area exists with the requested id.");
        }

        return StatusCode(StatusCodes.Status201Created, board);
    }

    /// <summary>
    /// The creator, taken from the validated token. The request body has no say in this — a client
    /// that sends its own id is simply not modelled by <see cref="CreateBoardRequestDto"/>.
    /// </summary>
    private int GetAuthenticatedUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Every token this server issues carries the claim, so its absence means the server is
        // misconfigured — not that the caller did anything wrong. A 500 states that honestly.
        if (!int.TryParse(claim, out var userId))
        {
            throw new InvalidOperationException(
                "The authenticated token carries no usable NameIdentifier claim.");
        }

        return userId;
    }
}
