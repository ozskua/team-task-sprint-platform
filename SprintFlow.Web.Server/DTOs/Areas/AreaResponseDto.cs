using SprintFlow.Web.Server.DTOs.Boards;

namespace SprintFlow.Web.Server.DTOs.Areas;

public record AreaResponseDto(
    int AreaId,
    string Name,
    int CreatedByUserId,
    string CreatedByFullName,
    DateTime CreatedDate,
    IReadOnlyList<BoardResponseDto> Boards);
