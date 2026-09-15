using SprintFlow.Web.Server.DTOs.Areas;
using SprintFlow.Web.Server.Models;

namespace SprintFlow.Web.Server.Mappings;

public static class AreaMappings
{
    public static AreaResponseDto ToResponseDto(this Area area)
        => new(
            area.AreaId,
            area.Name,
            area.CreatedByUserId,
            area.CreatedByUser.FullName,
            area.CreatedDate,
            area.Boards
                .OrderBy(board => board.BoardId)
                .Select(board => board.ToResponseDto())
                .ToList());
}
