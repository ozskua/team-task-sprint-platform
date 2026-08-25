using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.Extensions;
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
            User.GetAuthenticatedUserId(),
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
}
