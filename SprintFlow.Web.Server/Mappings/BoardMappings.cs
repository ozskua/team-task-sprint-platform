using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class BoardMappings
{
    /// <summary>Requires <c>CreatedByUser</c> to be loaded; the creator's name is read from it.</summary>
    public static BoardResponseDto ToResponseDto(this Board board)
        => new(
            board.BoardId,
            board.Name,
            board.AreaId,
            board.CreatedByUserId,
            board.CreatedByUser.FullName,
            board.CreatedDate);

    /// <summary>
    /// The whole board page. Requires the detail graph to be loaded — creator, columns, tasks,
    /// task creators and participants.
    /// </summary>
    /// <param name="board">The board, with its detail graph loaded.</param>
    /// <param name="members">
    /// Computed from participation and passed in, because membership is not part of the board's
    /// object graph — there is no membership table to include.
    /// </param>
    /// <param name="selectedSprint">
    /// The sprint the tasks were filtered to, or <c>null</c> for the whole board. Passed in for the
    /// same reason as the members: its status and task count are worked out elsewhere.
    /// </param>
    public static BoardDetailResponseDto ToDetailResponseDto(
        this Board board,
        IReadOnlyList<User> members,
        SprintResponseDto? selectedSprint)
        => new(
            board.BoardId,
            board.Name,
            board.AreaId,
            board.CreatedByUserId,
            board.CreatedByUser.FullName,
            board.CreatedDate,
            members
                .Select(member => new TeamMemberResponseDto(member.UserId, member.FullName))
                .ToList(),
            selectedSprint,
            board.WorkflowColumns.ToOrderedResponseDtos(
                board.WorkflowColumns
                    .SelectMany(column => column.Tasks)
                    .ToLookup(task => task.TaskColumnId)));
}
