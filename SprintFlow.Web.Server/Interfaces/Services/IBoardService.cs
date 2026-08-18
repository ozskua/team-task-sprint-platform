using SprintFlow.Web.Server.DTOs.Boards;

namespace SprintFlow.Web.Server.Interfaces.Services;

public interface IBoardService
{
    /// <summary>
    /// Creates a Board in the given Area, together with the Board's one Team.
    /// </summary>
    /// <param name="createdByUserId">
    /// Resolved by the controller from the caller's token. Passed in rather than read from
    /// <c>HttpContext</c> so this layer stays free of the request.
    /// </param>
    /// <returns>
    /// The created Board, or <c>null</c> when no Area has that id. Any authenticated user may add a
    /// Board to any Area, so a missing Area is the only reason this can fail to produce one.
    /// </returns>
    Task<BoardResponseDto?> CreateAsync(
        int areaId,
        CreateBoardRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Renames the board. Returns <c>null</c> when no such board exists. The name is the only thing
    /// a board has that can be edited.
    /// </summary>
    Task<BoardResponseDto?> RenameAsync(
        int boardId,
        CreateBoardRequestDto request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the board. <c>false</c> when no such board exists. Everything below it goes by
    /// cascade — see the schema, not this method.
    /// </summary>
    Task<bool> DeleteAsync(int boardId, CancellationToken cancellationToken);
}
