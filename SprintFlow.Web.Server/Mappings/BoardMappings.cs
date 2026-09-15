using SprintFlow.Web.Server.DTOs.Boards;
using SprintFlow.Web.Server.DTOs.Sprints;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class BoardMappings
{
    public static BoardResponseDto ToResponseDto(this Board board)
        => new(
            board.BoardId,
            board.Name,
            board.AreaId,
            board.CreatedByUserId,
            board.CreatedByUser.FullName,
            board.CreatedDate);

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
