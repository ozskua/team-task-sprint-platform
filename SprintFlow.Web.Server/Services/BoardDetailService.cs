using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.Interfaces.Repositories;
using SprintFlow.Web.Server.Interfaces.Services;
using SprintFlow.Web.Server.Mappings;

namespace SprintFlow.Web.Server.Services;

/// <summary>
/// Assembles the board page. Separate from <see cref="BoardService"/>, which creates boards: the
/// two share no logic and putting them together would only mean one class doing two jobs.
/// </summary>
public class BoardDetailService : IBoardDetailService
{
    private readonly IBoardRepository boardRepository;
    private readonly ITaskParticipantRepository participantRepository;

    public BoardDetailService(
        IBoardRepository boardRepository,
        ITaskParticipantRepository participantRepository)
    {
        this.boardRepository = boardRepository;
        this.participantRepository = participantRepository;
    }

    public async Task<BoardDetailResponseDto?> GetAsync(int boardId, CancellationToken cancellationToken)
    {
        var board = await boardRepository.GetDetailAsync(boardId, cancellationToken);

        if (board is null)
        {
            return null;
        }

        // A second query rather than part of the graph above, because membership is not a
        // relationship the board has — it is worked out from who participates in its tasks.
        var members = await participantRepository.GetBoardMembersAsync(boardId, cancellationToken);

        return board.ToDetailResponseDto(members);
    }
}
