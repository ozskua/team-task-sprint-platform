using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Services;

public class BoardService : IBoardService
{
    private readonly IBoardRepository boardRepository;
    private readonly IAreaRepository areaRepository;

    public BoardService(IBoardRepository boardRepository, IAreaRepository areaRepository)
    {
        this.boardRepository = boardRepository;
        this.areaRepository = areaRepository;
    }

    public async Task<BoardResponseDto?> CreateAsync(
        int areaId,
        CreateBoardRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken)
    {
        // The only check there is. Deliberately not "is the caller the Area's owner": any
        // authenticated user may add a Board to any Area, and ownership grants nothing.
        if (!await areaRepository.ExistsAsync(areaId, cancellationToken))
        {
            return null;
        }

        var board = new Board
        {
            AreaId = areaId,

            // Stored without surrounding whitespace. A name that is only whitespace never reaches
            // here — [Required] rejects it.
            Name = request.Name.Trim(),
            CreatedByUserId = createdByUserId,

            // Every Board has exactly one Team, created with it and never by hand. Assigning it
            // here means both rows go in on the same save.
            Team = new Team
            {
                // Inherited from the Board's creator. Ownership information; it grants nothing.
                CreatedByUserId = createdByUserId
            },

            // The three required columns, assigned the same way and for the same reason: they go in
            // on the same save, so a Board can never exist without them.
            WorkflowColumns = BuildSystemColumns()

            // CreatedDate is intentionally not set on any of them; the database defaults supply them.
        };

        var boardId = await boardRepository.AddAsync(board, cancellationToken);

        // Re-read so the response carries the creator's name and the generated date from the
        // database rather than anything assembled here.
        var created = await boardRepository.GetByIdAsync(boardId, cancellationToken)
            ?? throw new InvalidOperationException($"Board {boardId} was saved but could not be read back.");

        return created.ToResponseDto();
    }

    public async Task<BoardResponseDto?> RenameAsync(
        int boardId,
        CreateBoardRequestDto request,
        CancellationToken cancellationToken)
    {
        var board = await boardRepository.GetForUpdateAsync(boardId, cancellationToken);

        if (board is null)
        {
            return null;
        }

        // Same rule as creating one: stored without surrounding whitespace, and a name that is only
        // whitespace never reaches here because [Required] rejects it. Names are not unique.
        board.Name = request.Name.Trim();

        await boardRepository.SaveChangesAsync(cancellationToken);

        var renamed = await boardRepository.GetByIdAsync(boardId, cancellationToken)
            ?? throw new InvalidOperationException($"Board {boardId} was saved but could not be read back.");

        return renamed.ToResponseDto();
    }

    public async Task<bool> DeleteAsync(int boardId, CancellationToken cancellationToken)
    {
        var board = await boardRepository.GetForUpdateAsync(boardId, cancellationToken);

        if (board is null)
        {
            return false;
        }

        boardRepository.Remove(board);

        // One delete. The Team, the workflow columns, the tasks and their assignments follow from
        // the cascades already proven in the schema, so nothing is removed row by row.
        await boardRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// The three columns every Board is required to have, in the order the board displays them:
    /// To Do first, then any custom columns the user adds later, then Completed and Cancelled.
    /// </summary>
    /// <remarks>
    /// A fresh list each call, because EF attaches these instances to the Board being created.
    /// <para>
    /// The same three names and positions also appear in the migration that backfilled existing
    /// boards. That duplication is deliberate: a migration records what happened at a point in
    /// time, so it must not read a constant that a later release could change underneath it.
    /// </para>
    /// </remarks>
    private static List<WorkflowColumn> BuildSystemColumns()
        =>
        [
            new WorkflowColumn { Name = "To Do", Position = 0, SystemKind = SystemColumnKind.ToDo },
            new WorkflowColumn { Name = "Completed", Position = 1, SystemKind = SystemColumnKind.Completed },
            new WorkflowColumn { Name = "Cancelled", Position = 2, SystemKind = SystemColumnKind.Cancelled }
        ];
}
