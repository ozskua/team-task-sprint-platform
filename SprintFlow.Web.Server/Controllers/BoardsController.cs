using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Services;

namespace SprintFlow.Web.Server.Controllers;

[ApiController]
[Route("api/areas/{areaId:int}/boards")]
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly BoardService boardService;

    public BoardsController(BoardService boardService)
    {
        this.boardService = boardService;
    }

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
